using CommuniZEN.ViewModels;
namespace CommuniZEN.Views;

public partial class DailyAffirmationsPage : ContentPage
{

    public DailyAffirmationsPage()
	{
		InitializeComponent();
        BindingContext = new DailyAffirmationsViewModel(Navigation);
    }
}