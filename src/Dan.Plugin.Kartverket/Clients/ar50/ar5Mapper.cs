using Dan.Plugin.Kartverket.Models;
using System;

namespace Dan.Plugin.Kartverket.Clients.ar50
{
    public static class Ar5Mapper
    {

        public static string MapArealType(string arealType)
        {
            if (string.IsNullOrWhiteSpace(arealType))
                return ArealType.IkkeKartlagt.ToString();

            if (Enum.TryParse<ArealType>(arealType, ignoreCase: true, out var result))
                return result.ToString();

            return ArealType.IkkeKartlagt.ToString();
        }
    }
}
