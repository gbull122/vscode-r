using Microsoft.R.Host.Client;
using Prism.Ioc;
using Prism.Unity;
using System;
using System.Threading.Tasks;
using System.Windows;
using TNCodeApp.R;

namespace TestApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        private IRManager rManager;

        protected override Window CreateShell()
        {
            var mainWindow = Container.Resolve<MainWindow>();

            return mainWindow;
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            rManager = new RManager(new RHostSessionCallback());
            containerRegistry.RegisterInstance<IRManager>(rManager);

            Setup();
        }

        public async Task Setup()
        {
            await rManager.InitialiseAsync();

            var v = await rManager.RVersionFromConnectedRAsync();
        }
    }
}
