using System.Collections.Generic;
using System.Linq;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Modules.HashModule
{
    internal static class HashLogic
    {
        private static Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public static List<WriterDescription> GetDestination(int countReplics, string current, List<HashMapRecord> map)
        {
            var ret = new List<WriterDescription>();
            for (int i = 0; i < countReplics; i++)
            {
                var find =
                    map.FirstOrDefault(
                        x => HashComparer.Compare(current, x.End) <= 0 && !ret.Contains(x.ServerId));

                if (find == null && map.Count > 0)
                {
                    current = Consts.StartHashInRing;
                    find =
                        map.FirstOrDefault(
                            x => HashComparer.Compare(current, x.End) <= 0 && !ret.Contains(x.ServerId));
                }

                if (find == null)
                {
                    _logger.Error(Errors.NotEnoughServers);
                    ret.Clear();
                    break;
                }
                current = find.End;
                ret.Add(find.ServerId);
            }

            return ret;
        }
    }
}