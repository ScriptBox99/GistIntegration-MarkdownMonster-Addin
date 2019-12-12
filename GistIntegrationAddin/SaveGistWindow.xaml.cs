﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FontAwesome.WPF;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MarkdownMonster;
using MarkdownMonster.Windows;
using Westwind.Utilities;

namespace GistIntegration
{
    /// <summary>
    /// Interaction logic for LoadAndSaveGist.xaml
    /// </summary>
    public partial class SaveGistWindow
    {

        public LoadAndSaveGistModel Model { get; set; }
        private PasteCodeAsGistAddin Addin { get; }

        public StatusBarHelper Status { get; set; }

        public SaveGistWindow(PasteCodeAsGistAddin addin)
        {
            Addin = addin;

            InitializeComponent();

            mmApp.SetThemeWindowOverride(this);
            
            Model = new LoadAndSaveGistModel(addin)
            {
                Configuration = PasteCodeAsGistConfiguration.Current,
                GistUsername = PasteCodeAsGistConfiguration.Current.GithubUsername
            };
            Model.PropertyChanged += (o, args) =>
            {
                if (args.PropertyName == "GistUsername")
                {                    
                    Task t = Model.LoadGists(this);
                }                    
            };
            DataContext = Model;

            Status = new StatusBarHelper(StatusText, StatusIcon);

            Loaded += SaveGistWindow_Loaded;
        }

        private async void SaveGistWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Dispatcher.InvokeAsync( async () =>
            {
                await Model.LoadGists(this);
            }, System.Windows.Threading.DispatcherPriority.Background);
        }
        

        
        private void ButtonSaveGist_Click(object sender, RoutedEventArgs e)
        {
            Model.ActiveItem.code = Addin.GetMarkdown();

            GistItem gist;

            Status.ShowStatus("Saving Gist...");
            if (!Model.SaveAsNewGist)
            {                
                gist = GistClient.UpdateGist(Model.ActiveItem, Model.Configuration.GithubUserToken);
            }
            else
                gist = GistClient.PostGist(Model.ActiveItem, Model.Configuration.GithubUserToken);

            if (gist != null && !gist.hasError)
            {
                Status.ShowStatus("Gist has been saved...", 5000);                
                mmFileUtils.ShowExternalBrowser(gist.htmlUrl);
                Close();
            }
            else
            {
                mmApp.Log(gist.errorMessage);

                Status.SetStatusIcon(FontAwesomeIcon.ExclamationCircle, Colors.Firebrick);
                Status.ShowStatus("Failed to save as Gist. Refer to error log for more detail.", 7000);
            }

        }


        private void ButtonDeleteGist_Click(object sender, RoutedEventArgs e)
        {
            var gist = ((Button)sender).DataContext as GistItem;
            if (gist == null)
                return;

            var msg =
                $@"Filename: {gist.filename}
Description: {gist.description}

Are you sure you want to delete this Gist?";

            var res = MessageBox.Show(msg, "Delete Gist", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.No)
                return;

            if (!GistClient.DeleteGist(gist.id))
            {
                Status.ShowStatus("Failed to delete Gist.", 7000);
                Status.SetStatusIcon(FontAwesomeIcon.Warning, Colors.Red);
            }
            else
            {
                Model.GistList.Remove(gist);
                Status.ShowStatus("Gist Deleted.", 7000);
            }
        }

        

    }
}

