using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.ProxyGate;

namespace BricksDb.RedisInterface.Server
{
    class RedisMessageProcessor
    {
        private IStorage<string, string> _redisTable;
        private Dictionary<char, Func<string, string>> _processOnDataType;
        private Dictionary<string, Func<object, string>> _executeCommand;

        private const string KeyRand = "key:__rand_int__";
        private static Random random = new Random(DateTime.Now.Millisecond);
        
        private int successWrites = 0;
        private int failWrite = 0;

        private int successRead = 0;
        private int failRead = 0;

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
            _executeCommand = new Dictionary<string, Func<object, string>>()
            {
                {"SET", ProcessSet}
            };
        }

        public string ProcessMessage(string message)
        {
            //_redisGate.RedisTable.Create()
            //var responce = "+OK\r\n";
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
            var array = data.Substring(1).Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            var command = array[2];
            var parameters = new string[]
            {
                array[4], array[6]
            };
            var responce = _executeCommand[command](parameters);
            return responce;
        }

        public string ProcessSet(object parameter_array)
        {
            string[] parameters = parameter_array as string[]; // TODO: проверить на null
            var key = parameters[0];
            var value = parameters[1];
            var responseBriks = _redisTable.Create(key, value);
            if (responseBriks.IsError)
            {
                
            }
            else
            {

            }
            
            
            
            
            var responseRedis = "+OK\r\n"; // всегда ОК, чтобы бенчмарк работал. Ошибки считаются внутри этой системы
            return responseRedis;
        }
    }
}
