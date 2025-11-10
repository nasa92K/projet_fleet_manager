
namespace projet_fleet_manager
{
    internal class MySqlCommand
    {
        private string query;
        private MySqlConnection connection;

        public MySqlCommand(string query, MySqlConnection connection)
        {
            this.query = query;
            this.connection = connection;
        }

        public object Parameters { get; internal set; }

        internal bool ExecuteScalar()
        {
            throw new NotImplementedException();
        }
    }
}