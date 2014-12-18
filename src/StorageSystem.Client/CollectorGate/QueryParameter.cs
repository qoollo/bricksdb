namespace Qoollo.Client.CollectorGate
{
    public class QueryParameter
    {
        /// <summary>
        /// Описание параметра при запросе
        /// </summary>
        /// <param name="name">Имя параметра</param>
        /// <param name="value">Значение параметра</param>
        /// <param name="type">Тип параметра</param>
        public QueryParameter(string name, object value, object type)
        {
            Type = type;
            Value = value;
            Name = name.Replace("@","");
        }

        public string Name { get; private set; }
        public object Value { get; private set; }
        public object Type { get; private set; }
    }
}
