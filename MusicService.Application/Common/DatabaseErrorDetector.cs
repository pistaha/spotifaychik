using System;
using System.Linq;

namespace MusicService.Application.Common
{
    public static class DatabaseErrorDetector
    {
        public static bool IsUniqueViolation(Exception ex)
        {
            var sqlState = GetSqlState(ex);
            if (sqlState == "23505")
            {
                return true; // Postgres unique_violation
            }

            var number = GetSqlServerNumber(ex);
            if (number == 2601 || number == 2627)
            {
                return true; // SQL Server duplicate key
            }

            var sqliteExtendedCode = GetSqliteExtendedErrorCode(ex);
            if (sqliteExtendedCode == 1555 || sqliteExtendedCode == 2067)
            {
                return true; // SQLite primary key / unique constraint violation
            }

            return false;
        }

        public static bool IsForeignKeyViolation(Exception ex)
        {
            var sqlState = GetSqlState(ex);
            if (sqlState == "23503")
            {
                return true; // Postgres foreign_key_violation
            }

            var number = GetSqlServerNumber(ex);
            if (number == 547)
            {
                return true; // SQL Server foreign key violation
            }

            var sqliteExtendedCode = GetSqliteExtendedErrorCode(ex);
            if (sqliteExtendedCode == 787)
            {
                return true; // SQLite foreign key constraint failed
            }

            return false;
        }

        public static bool IsTransient(Exception ex)
        {
            var sqlState = GetSqlState(ex);
            if (sqlState == "40001" || sqlState == "40P01")
            {
                return true; // Postgres serialization_failure / deadlock_detected
            }

            var number = GetSqlServerNumber(ex);
            if (number == 1205)
            {
                return true; // SQL Server deadlock
            }

            return false;
        }

        private static string? GetSqlState(Exception ex)
        {
            var baseException = ex.GetBaseException();
            var property = baseException.GetType().GetProperty("SqlState");
            if (property?.PropertyType == typeof(string))
            {
                return property.GetValue(baseException) as string;
            }

            return null;
        }

        private static int? GetSqlServerNumber(Exception ex)
        {
            var baseException = ex.GetBaseException();
            var property = baseException.GetType().GetProperty("Number");
            if (property?.PropertyType == typeof(int))
            {
                return property.GetValue(baseException) as int?;
            }

            return null;
        }

        private static int? GetSqliteExtendedErrorCode(Exception ex)
        {
            var baseException = ex.GetBaseException();
            var property = baseException.GetType().GetProperty("SqliteExtendedErrorCode");
            if (property?.PropertyType == typeof(int))
            {
                return property.GetValue(baseException) as int?;
            }

            return null;
        }
    }
}
