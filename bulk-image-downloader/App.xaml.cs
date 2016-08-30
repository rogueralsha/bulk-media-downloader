using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace BulkMediaDownloader
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            try {
            using (DbContext db = new BulkMediaDownloader.Model.DatabaseContext()) {
                db.Database.Migrate();
            }
            } catch (Exception ex) {
                throw ex;
            }
        }
    }
}
