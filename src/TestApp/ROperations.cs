using Microsoft.R.Host.Client;
using Prism.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TNCodeApp.R
{
    public class ROperations : IROperations
    {
        private IRHostSession rHostSession;
        //private ILoggerFacade logger;

        public ROperations(IRHostSession session)
        {
            rHostSession = session;
            //logger = loggerFacade;
        }

        public bool IsHostRunning()
        {
            return rHostSession.IsHostRunning;
        }

        public async Task StartHostAsync(IRHostSessionCallback rHostSessionCallback)
        {
            await rHostSession.StartHostAsync(rHostSessionCallback);
        }

        public async Task ExecuteCommandAsync(string command)
        {
            await rHostSession.ExecuteAsync(command);
        }

        public async Task<DataFrame> GetDataFrameAsync(string name)
        {
            return await rHostSession.GetDataFrameAsync(name);
        }

        public async Task<RSessionOutput> ExecuteAndOutputAsync(string command)
        {
            return await rHostSession.ExecuteAndOutputAsync(command);
        }

        public async Task<List<object>> GetListAsync(string command)
        {
            return await rHostSession.GetListAsync(command);
        }

        public async Task<string> EvaluateAsync<T>(string command)
        {
            return await rHostSession.EvaluateAsync<string>(command);
        }

        public async Task ExecuteAsync(string command)
        {
            await rHostSession.ExecuteAsync(command);
        }

        public async Task CreateDataFrameAsync(string name, DataFrame dataFrame)
        {
            //logger.Log("Creating DataFrame: "+name, Category.Info, Priority.None);
            await rHostSession.CreateDataFrameAsync(name, dataFrame);
        }
    }
}
