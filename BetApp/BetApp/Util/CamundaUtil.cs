using Camunda.Api.Client;
using Camunda.Api.Client.Message;
using Camunda.Api.Client.ProcessDefinition;
using Camunda.Api.Client.ProcessInstance;
using Camunda.Api.Client.UserTask;
using BetApp.Models;
using Camunda.Api.Client.History;
using Camunda.Api.Client.User;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace BetApp.Util
{
    public class CamundaUtil
    {
        private const string camundaEngineUri = "http://localhost:8080/engine-rest";
        private static CamundaClient client = CamundaClient.Create(camundaEngineUri);
        private const string processKey = "BettingProcess";
        private const string applyMessage = "adminMessage";


        public static async Task<string> StartBetProcess(int betId, string user)
        {
            var parameters = new Dictionary<string, object>();
            parameters["BetId"] = betId;
            parameters["User"] = user;
            var processInstanceId = await StartProcess(parameters);
            return processInstanceId;
        }

        private static async Task<string> StartProcess(Dictionary<string, object> processParameters)
        {
            var client = CamundaClient.Create(camundaEngineUri);
            StartProcessInstance parameters = new StartProcessInstance();
            foreach (var param in processParameters)
            {
                parameters.SetVariable(param.Key, param.Value);
            }
            var definition = client.ProcessDefinitions.ByKey(processKey);
            ProcessInstanceWithVariables processInstance = await definition.StartProcessInstance(parameters);
            return processInstance.Id;
        }

        public static async Task<bool> IsUserInGroup(string user, string group)
        {
            var list = await client.Users
                                   .Query(new UserQuery
                                   {
                                       Id = user,
                                       MemberOfGroup = group
                                   })
                                   .List();
            return list.Count > 0;
        }

        public static async Task PickTask(string taskId, string user)
        {
            await client.UserTasks[taskId].Claim(user);
        }

        public static async Task FinishTask(string taskId) //ovo nikad ne ide 
        {
            await client.UserTasks[taskId].Complete(new CompleteTask());
        }

        public static async Task EnterBetAmount(string taskId, long betAmount)
        {
            var variables = new Dictionary<string, VariableValue>();
            variables["betAmount"] = VariableValue.FromObject(betAmount);
            await client.UserTasks[taskId].Complete(new CompleteTask()
            {
                Variables = variables
            });
        }

        public static async Task ApplyForBetReview(string pid, string user)
        {
            var message = new CorrelationMessage()
            {
                ProcessInstanceId = pid,
                MessageName = applyMessage,
                All = true,
                BusinessKey = null
            };
            message.ProcessVariables.Set("Admin", user);
            await client.Messages.DeliverMessage(message);
        }


        public static async Task ApproveBet(string taskId, bool approved)
        {
            var variables = new Dictionary<string, VariableValue>();
            variables["approved"] = VariableValue.FromObject(approved);
            if (!approved)
            {
                variables["Admin"] = VariableValue.FromObject(null);
            }
            await client.UserTasks[taskId].Complete(new CompleteTask()
            {
                Variables = variables
            });
        }

        public static async Task SetBetResult(string taskId, string result)
        {
            var variables = new Dictionary<string, VariableValue>();
            variables["betResult"] = VariableValue.FromObject(result == "Win");
            await client.UserTasks[taskId].Complete(new CompleteTask()
            {
                Variables = variables
            });
        }

        public static async Task<List<BetInfo>> GetBets()
        {
            var historyList = await client.History.ProcessInstances.Query(new HistoricProcessInstanceQuery { ProcessDefinitionKey = processKey }).List();
            var bets = historyList.OrderBy(p => p.StartTime)
                                  .Select(p => new BetInfo
                                  {
                                      StartTime = p.StartTime,
                                      EndTime = p.State == ProcessInstanceState.Completed ? p.EndTime : new DateTime?(),
                                      Ended = p.State == ProcessInstanceState.Completed,
                                      PID = p.Id
                                  })
                                  .ToList();

            var tasks = new List<Task>();
            foreach (var bet in bets)
            {
                tasks.Add(LoadInstanceVariables(bet));
            }
            await Task.WhenAll(tasks);

            return bets;
        }

        public static async Task<List<TaskInfo>> GetTasks(string username)
        {
            var userTasks = await client.UserTasks
                                        .Query(new TaskQuery
                                        {
                                            Assignee = username,
                                            ProcessDefinitionKey = processKey
                                        })
                                        .List();

            var list = userTasks.OrderBy(t => t.Created)
                                .Select(t => new TaskInfo
                                {
                                    TID = t.Id,
                                    TaskName = t.Name,
                                    TaskKey = t.TaskDefinitionKey,
                                    PID = t.ProcessInstanceId,
                                    StartTime = t.Created,
                                })
                                .ToList();

            var tasks = new List<Task>();
            foreach (var task in list)
            {
                tasks.Add(LoadTaskVariables(task));
            }
            await Task.WhenAll(tasks);
            return list;
        }

        public static async Task<List<TaskInfo>> UnAssignedGroupTasks(string groupName)
        {
            var userTasks = await client.UserTasks
                                        .Query(new TaskQuery
                                        {
                                            Assigned = false,
                                            CandidateGroup = groupName,
                                            ProcessDefinitionKey = processKey
                                        })
                                        .List();

            var list = userTasks.OrderBy(t => t.Created)
                                .Select(t => new TaskInfo
                                {
                                    TID = t.Id,
                                    TaskName = t.Name,
                                    TaskKey = t.TaskDefinitionKey,
                                    PID = t.ProcessInstanceId,
                                    StartTime = t.Created,
                                })
                                .ToList();

            var tasks = new List<Task>();
            foreach (var task in list)
            {
                tasks.Add(LoadTaskVariables(task));
            }
            await Task.WhenAll(tasks);
            return list;
        }

        private static async Task LoadTaskVariables(TaskInfo task)
        {
            var variables = await client.UserTasks[task.TID].Variables.GetAll();

            if (variables.TryGetValue("BetId", out VariableValue value))
            {
                task.BetId = value.GetValue<int>();
            }

            if (variables.TryGetValue("User", out value))
            {
                task.BetUser = value.GetValue<string>();
            }
        }

        private static async Task LoadInstanceVariables(BetInfo bet)
        {
            var list = await client.History.VariableInstances.Query(new HistoricVariableInstanceQuery { ProcessInstanceId = bet.PID }).List();
            bet.BetId = list.Where(v => v.Name == "BetId")
                            .Select(v => Convert.ToInt32(v.Value))
                            .First();

            bet.User = list.Where(v => v.Name == "User")
                           .Select(v => (string)v.Value)
                           .First();

            var admin = list.Where(v => v.Name == "Admin")
                         .Select(v => v.Value as string)
                         .FirstOrDefault();

            bet.Admin = admin;

            bet.BetAmount = list.Where(v => v.Name == "betAmount")
                                .Select(v => Convert.ToInt64(v.Value))
                                .FirstOrDefault();

            bet.CanApplyForReview = string.IsNullOrWhiteSpace(admin);

            bet.betResult = list.Where(v => v.Name == "betResult")
                        .Select(v => Convert.ToBoolean(v.Value))
                        .FirstOrDefault();

            var statusVariable = list.Where(v => v.Name == "status")
                             .Select(v => v.Value as string)
                             .FirstOrDefault();

            bet.Status = statusVariable ?? (bet.betResult ? "Won" : "Lost");
        }

    }
}
