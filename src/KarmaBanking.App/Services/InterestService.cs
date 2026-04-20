//using KarmaBanking.App.Repositories;
//using KarmaBanking.App.Repositories.Interfaces;
//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Windows.System;

//namespace KarmaBanking.App.Services
//{
//    public class InterestService
//    {
//        private readonly ISavingsRepository _repo;

//        public InterestService(ISavingsRepository repo)
//        {
//            _repo = repo;
//        }

//        public async Task ApplyMonthlyInterest(int userId)
//        {
//            var accounts = await _repo.GetSavingsAccountsByUserIdAsync(userId, includesClosed: false);

//            foreach (var acc in accounts.Where(a => a.AccountStatus == "Active"))
//            {
//                bool alreadyApplied = await _repo.HasInterestTransactionThisMonthAsync(acc.Id);
//                if (alreadyApplied) continue;

//                decimal interest = Math.Round(acc.Balance * acc.Apy / 12, 2);

//                if (interest > 0)
//                {
//                    await _repo.DepositAsync(acc.Id, interest, "Interest");
//                }
//            }
//        }
//    }
//}