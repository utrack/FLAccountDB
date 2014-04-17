using System.Collections.Generic;
using FLAccountDB.Data;

namespace FLAccountDB
{
    interface IDatabase
    {
        bool IsInitiated();
        List<Character> GetAccountsByID();



    }
}
