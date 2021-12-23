namespace PostgresEntities.Entities
{
    public class Server
    {
        public string Address { get; set; }

        public int Port { get; set; }
        public int PingPort { get; set; }

        public int Status { get; set; }

        public string Info { get; set; }
    }
}
