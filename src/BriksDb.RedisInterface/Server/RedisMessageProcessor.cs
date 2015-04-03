using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BricksDb.RedisInterface.Server.RedisOperations;
using Qoollo.Client.ProxyGate;

namespace BricksDb.RedisInterface.Server
{
    class RedisMessageProcessor
    {
        private IStorage<string, string> _redisTable;
        private Dictionary<char, Func<string, string>> _processOnDataType;
        private Dictionary<string, RedisOperation> _executeCommand;

        public RedisMessageProcessor(IStorage<string, string> redisTable)
        {
            _redisTable = redisTable;
            _processOnDataType = new Dictionary<char, Func<string, string>>()
            {
                /*{'+', ProcessStrings},
                {'-', ProcessErrors},
                {':', ProcessIntegers},
                {'$', ProcessBulk},*/
                {'*', ProcessArrays}
            };
            _executeCommand = new Dictionary<string, RedisOperation>()
            {
                {"SET", new RedisSet(_redisTable)},
                {"GET", new RedisGet(_redisTable)}
            };
        }

        public string ProcessMessage(string message)
        {
            var responce = _processOnDataType[message[0]](message); 
            return responce;
        }

        public string ProcessStrings(string data)
        {
            return "";
        }

        public string ProcessErrors(string data)
        {
            return "";
        }

        public string ProcessIntegers(string data)
        {
            return "";
        }

        public string ProcessBulk(string data)
        {
            return "";
        }

        public string ProcessArrays(string data)
        {
            // взять все сообщение, кроме 1го знака, который отвечает за тип
            // TODO: реализовать GET, количество аргументов в array[1] наверно. засунуть их в parameters[] и послать в операцию
            var array = data.Substring(1).Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            var numParams = Convert.ToInt32(array[0])-1;
            var command = array[2];
            var parameters = new string[numParams];
            for (int i = 0; i < numParams; i++)
            {
                parameters[i] = array[4 + i*2];
            }
            var response = _executeCommand[command].PerformOperation(parameters);
            return response;
        }

        
    }
}
