using System.ComponentModel;
using System.Runtime.CompilerServices;
using KarmaBanking.App.Models;
using KarmaBanking.App.Repositories.Interfaces;

namespace KarmaBanking.App.ViewModels
{
    public class InvestmentsViewModel : INotifyPropertyChanged
    {
        private readonly IInvestmentRepository _repo;

        public InvestmentsViewModel(IInvestmentRepository repo)
        {
            _repo = repo;
            portfolio = new Portfolio();
        }

        private Portfolio _portfolio;
        public Portfolio portfolio
           {
               get => _portfolio;
               set
                    {
                       _portfolio = value;
                       OnPropertyChanged();
                    }
            }

        private bool _isLoading;
        public bool isLoading
        {
               get => _isLoading;
               set
                {
                   _isLoading = value;
                   OnPropertyChanged();
                 }
        }

        public void loadPortfolio()
        {
            isLoading = true;
           

            try
            {
                portfolio = _repo.GetPortfolio(1);
               
            }
            catch (Exception ex)
             {
                System.Diagnostics.Debug.WriteLine($"loadPortfolio error: {ex.Message}");
             }
            finally
            {
                isLoading = false;
                
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
