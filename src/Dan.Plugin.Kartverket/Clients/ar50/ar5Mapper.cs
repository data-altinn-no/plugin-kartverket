using Dan.Plugin.Kartverket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dan.Plugin.Kartverket.Clients.ar50
{
    public static class ar5Mapper
    {

        public static ArealType MapArealType(string arealType)
        {
            if (string.IsNullOrWhiteSpace(arealType))
            {
                return ArealType.IkkeKartlagt;
            }

            // Try to parse as integer first
            if (int.TryParse(arealType, out int arealTypeValue))
            {
                return arealTypeValue switch
                {
                    11 => ArealType.Bebygd,
                    12 => ArealType.Samferdsel,
                    21 => ArealType.FulldyrkaJord,
                    22 => ArealType.OverflateDyrkaJord,
                    23 => ArealType.Innmarksbeite,
                    30 => ArealType.Skog,
                    50 => ArealType.ApenFastmark,
                    60 => ArealType.Myr,
                    70 => ArealType.Bre,
                    81 => ArealType.Ferskvann,
                    82 => ArealType.Hav,
                    99 => ArealType.IkkeKartlagt,
                    _ => ArealType.IkkeKartlagt
                };
            }

            // Fallback: try to parse enum by name
            if (Enum.TryParse<ArealType>(arealType, true, out ArealType result))
            {
                return result;
            }

            return ArealType.IkkeKartlagt;
        }
    }
}
