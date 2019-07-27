using System;
using MySql.Data.MySqlClient;
using System.Data;


namespace PoolVision
{
    public class MySqlClient {
        private MySqlConnection sql;

        public DataRow dbRow( String query ) {
            MySqlCommand command = sql.CreateCommand();
            command.CommandText = query;

            DataTable selectDT = new DataTable();
            MySqlDataAdapter dataAd = new MySqlDataAdapter( command );

            dataAd.Fill( selectDT );

            if( selectDT.Rows.Count > 0 )
                return selectDT.Rows[0];
            else
                return null;
        }

        public int lastInsertId() {
            DataRow r = dbRow( "SELECT last_insert_id() as lid" );

            UInt64 id = (UInt64)r[0];

            return (int)id;
        }

        public int affectedRows() {
            DataRow r = dbRow( "SELECT ROW_COUNT()" );
            int id = (int)r[0];

            return id;
        }

        public DataTable dbResult( String query ) {
            var command = sql.CreateCommand();
            command.CommandText = query;
            using (var dataAd = new MySqlDataAdapter(command))
            {

                var selectDT = new DataTable();
                dataAd.Fill(selectDT);

                return selectDT;
            }

        }

        internal int dbNone( string query ) {
            var command = sql.CreateCommand();
            //MySqlDataReader Reader;
            command.CommandText = query;
            return command.ExecuteNonQuery();
        }

        public MySqlClient( String SqlConString ) {
            sql = new MySqlConnection( SqlConString );
            sql.Open();
        }

        public DateTime ConvertFromUnixTimestamp( double timestamp ) {
            DateTime origin = new DateTime( 1970, 1, 1, 0, 0, 0, 0 );
            return origin.AddSeconds( timestamp );
        }

        public double ConvertToUnixTimestamp( DateTime date ) {
            DateTime origin = new DateTime( 1970, 1, 1, 0, 0, 0, 0 );
            TimeSpan diff = date - origin;
            return Math.Floor( diff.TotalSeconds );
        }

        public int InsertCard(string Name, string pHash, string Set, string Type, string Cost, string Rarity, Guid ScryfallId)
        {
            MySqlCommand cmd = sql.CreateCommand();
            cmd.CommandText = "INSERT INTO `cards` (`Name`, `pHash`, `Set`, `Type`, `Cost`, `Rarity`, `sf_id`) VALUES (?name, ?phash, ?set, ?type, ?cost, ?rarity, ?sf_id)";
            cmd.Parameters.Add("?name", MySqlDbType.VarChar).Value = Name;
            cmd.Parameters.Add("?phash", MySqlDbType.VarChar).Value = pHash;
            cmd.Parameters.Add("?set", MySqlDbType.VarChar).Value = Set;
            cmd.Parameters.Add("?type", MySqlDbType.VarChar).Value = Type;
            cmd.Parameters.Add("?cost", MySqlDbType.VarChar).Value = Cost;
            cmd.Parameters.Add("?rarity", MySqlDbType.VarChar).Value = Rarity;
            cmd.Parameters.Add("?sf_id", MySqlDbType.VarChar).Value = ScryfallId;
            cmd.ExecuteNonQuery();

            return lastInsertId();
        }
    }
}
