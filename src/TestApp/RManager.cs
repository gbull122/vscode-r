using Microsoft.R.Host.Client;
using Prism.Events;
using Prism.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TNCodeApp.R
{
    public class RManager : IRManager
    {
        private IROperations rOperations;
        private readonly IRHostSessionCallback rHostSessionCallback;
        //private ILoggerFacade logger;
        //private IEventAggregator eventAggregator;

        public event EventHandler RConnected;
        public event EventHandler RDisconnected;

        public string WindowsDirectory
        {
            get;
            set;
        }

        public bool IsRRunning
        {
            get
            {
                return rOperations != null && rOperations.IsHostRunning();
            }
        }

        public RManager(IRHostSessionCallback rhostSession)
        {
            //logger = loggerFacade;
            rHostSessionCallback = rhostSession;
            //eventAggregator = evtAggregator;

            WindowsDirectory = Path.GetTempPath();
        }

        public RManager(IRHostSessionCallback rhostSession, ILoggerFacade loggerFacade, IROperations rOps)
        {
            //logger = loggerFacade;
            rHostSessionCallback = rhostSession;
            rOperations = rOps;
        }

        public async Task<bool> InitialiseAsync()
        {
            //try
            //{
                //logger.Log("Connecting to R...", Category.Info, Priority.None);

                var rHostSession = RHostSession.Create("TestApp");
                //rHostSession.Connected += RHostSession_Connected;
                //rHostSession.Disconnected += RHostSession_Disconnected;
                rOperations = new ROperations(rHostSession);
                
                await rOperations.StartHostAsync(rHostSessionCallback);
                //await rOperations.ExecuteAsync("library(" + string.Format("\"{0}\"", "R.devices") + ")");
                //await rOperations.ExecuteAsync("library(" + string.Format("\"{0}\"", "ggplot2") + ")");

                await rOperations.ExecuteAndOutputAsync("setwd(" + ConvertPathToR(WindowsDirectory) + ")");
                //logger.Log("Connected to R", Category.Info, Priority.None);

            //}
            //catch (Exception ex)
            //{
            //    //logger.Log("Failed to connect to R: " +ex.Message, Category.Exception, Priority.High);
            //    return false;
            //}

            return true;
        }

        private void RHostSession_Disconnected(object sender, EventArgs e)
        {
            OnRaiseRDisconnected();
        }

        public void OnRaiseRDisconnected()
        {
            RDisconnected?.Invoke(this, new EventArgs());
        }

        private void RHostSession_Connected(object sender, EventArgs e)
        {
            OnRaiseRConnected();
        }

        public void OnRaiseRConnected()
        {
            RConnected?.Invoke(this, new EventArgs());
        }

        public async Task DeleteVariablesAsync(List<string> names)
        {
            foreach (string name in names)
            {
                await rOperations.ExecuteAsync("rm(" + name + ")");
            }
        }

        public async Task<DataFrame> GetDataFrameAsync(string name)
        {
            DataFrame result = null;
            try
            {
                result = await rOperations.GetDataFrameAsync(name);
            }
            catch (Exception ex)
            {
                //logger.Log(ex.Message, Category.Exception, Priority.High);
            }
            return result;
        }

        public async Task<DataFrame> GetDataFrameAsDataSetAsync(string name)
        {
            var dataFrame = await GetDataFrameAsync(name);

            return dataFrame;
        }

        public async Task<object[,]> GetRListAsync(string name)
        {
            object[,] result = null;
            var listNames = await rOperations.ExecuteAndOutputAsync("names(" + name + ")");
            var names = listNames.Output.Replace("\"", "");
            var thing = names.Split(' ');
            thing = thing.Skip(1).ToArray();
            List<List<object>> data = new List<List<object>>();
            for (int i = 0; i < thing.Count(); i++)
            {
                var rlist = await rOperations.GetListAsync(name + "$" + thing[i]);
                data.Add(rlist);
            }
            result = ListOfVectorsToObject(data, thing);
            return result;
        }

        public object[,] ListOfVectorsToObject(List<List<object>> data, string[] names)
        {
            var maxLength = MaximumLengthOfList(data);

            object[,] result;
            int startRow = 0;
            if (names != null)
            {
                result = new object[maxLength + 1, data.Count];
                startRow = 1;
                for (int s = 0; s < names.Length; s++)
                {
                    result[0, s] = names[s];
                }
            }
            else
            {
                result = new object[maxLength, data.Count];
            }


            for (int column = 0; column < data.Count; column++)
            {
                for (int row = startRow; row < data[column].Count + startRow; row++)
                {
                    result[row, column] = data[column][row - startRow];
                }
            }
            return result;
        }

        public int MaximumLengthOfList(List<List<object>> data)
        {
            int maxLength = 0;
            foreach (var myVector in data)
            {
                if (myVector.Count > maxLength)
                {
                    maxLength = myVector.Count;
                }
            }
            return maxLength;
        }

        public async Task<string> RunRCommnadAsync(string code)
        {
            if (!rOperations.IsHostRunning())
                return string.Empty;

            return await rOperations.EvaluateAsync<string>(code);
        }

        public async Task<string> RHomeFromConnectedRAsync()
        {
            return await RunRCommnadAsync("R.home()");
        }

        public async Task<string> RPlatformFromConnectedRAsync()
        {
            return await RunRCommnadAsync("version$platform");
        }

        public async Task<string> RMinorVersionFromConnectedRAsync()
        {
            return await RunRCommnadAsync("R.version$minor");
        }

        public async Task<string> RVersionFromConnectedRAsync()
        {
            return await RunRCommnadAsync("R.version$version.string");
        }

        public async Task<bool> GenerateGgplotAsync(string ggplotCommand)
        {
            try
            {
                using (StringReader reader = new StringReader(ggplotCommand))
                {
                    var plotWidth = 15;
                    var plotHeight = 12;
                    var plotRes = 600;
                    //get code into text file
                    string fileName = WindowsDirectory + "\\TNGgplot.R";
                    using (StreamWriter file = new StreamWriter(fileName))
                    {
                        file.WriteLine(
                            "devEval(" + string.Format("\"{0}\"", "png") +
                            ", path = " + ConvertPathToR(WindowsDirectory) +
                            ", name = \"TNGgplot\", width = " + plotWidth +
                            ", height = " + plotHeight + ", units =" +
                            string.Format("\"{0}\"", "cm") + ", res = " + plotRes + ", pointsize = 12, {");

                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            file.WriteLine(line);
                        }
                        file.WriteLine("plot(p)");
                        file.WriteLine("})");
                    }

                    await rOperations.ExecuteAsync("source(" + ConvertPathToR(fileName) + ",echo=TRUE, max.deparse.length=10000)");
                }
            }
            catch
            {
                //ErrorMessage = "Failed to generate plot " + ex.Message;
                return false;
            }
            return true;
        }

        public string ConvertPathToR(string path)
        {
            string temp = path.Replace('\\', '/');
            return string.Format("\"{0}\"", temp);
        }

        public async Task<List<object>> ListWorkspaceItems()
        {
            await rOperations.ExecuteAsync("vars<-ls()");
            return await rOperations.GetListAsync("vars");
        }

        public async Task LoadToTempEnv(string fullFileName)
        {
            var rPath = ConvertPathToR(fullFileName);

            await rOperations.ExecuteAsync("tempEnv<-new.env()");
            await rOperations.ExecuteAsync("load(" +rPath+ ",envir=tempEnv)");
        }

        public async Task<List<object>> TempEnvObjects()
        {
            await rOperations.ExecuteAsync("tempVars<-ls(tempEnv)");
            return await rOperations.GetListAsync("tempVars");
        }

        public async Task RemoveTempEnviroment()
        {
            await rOperations.ExecuteAsync("rm(tempEnv)");
        }

        public async Task<bool> IsDataFrame(string name)
        {
            var classProps =  await rOperations.GetListAsync("class(tempEnv$" + name+")");
            foreach (string prop in classProps)
            {
                if (prop.Equals("data.frame"))
                {
                    await rOperations.ExecuteAsync(name+"<-tempEnv$"+name);
                    await rOperations.ExecuteAsync("rm(" + name + ",envir=tempEnv)");
                    return true;
                }
                   
            }
            return false;
        }

        public async Task LoadRWorkSpace( IProgress<string> progress, string fileName)
        {
            progress.Report("Loading workspace");

            await LoadToTempEnv(fileName);

            var tempItems = await TempEnvObjects();
            var workspaceItems = await ListWorkspaceItems();

            foreach (string tempItem in tempItems)
            {
                ///TODO give option to overwrite
                if (!workspaceItems.Contains(tempItem))
                {
                    var isDataFrame = await IsDataFrame(tempItem);
                    if (isDataFrame)
                    {
                        progress.Report("Loading dataframe " + tempItem);
                        var importedData = await GetDataFrameAsDataSetAsync(tempItem);

                        //var dataSetEventArgs = new DataSetEventArgs();
                        //dataSetEventArgs.Modification = DataSetChange.AddedFromR;
                        //dataSetEventArgs.Data = importedData;

                        //eventAggregator.GetEvent<DataSetChangedEvent>().Publish(dataSetEventArgs);
                    }
                }
            }

            await RemoveTempEnviroment();
        }

    }
}
