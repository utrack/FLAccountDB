using System.Collections.Generic;

namespace FLAccountDB
{
    interface IDatabase
    {
        bool IsInitiated();
        List<Character> GetAccountsByID();



    }
}
