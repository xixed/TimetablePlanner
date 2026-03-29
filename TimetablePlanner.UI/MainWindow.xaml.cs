using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TimetablePlanner.Core.Constraints.Hard_Constraints;
using TimetablePlanner.Core.Generators;
using TimetablePlanner.Core.Interfaces;
using TimetablePlanner.Core.Models;
using TimetablePlanner.Core.Services;
using TimetablePlanner.Data.Context;
using TimetablePlanner.Data.Repositories;


namespace TimetablePlanner.UI;


public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
    }
    


}
