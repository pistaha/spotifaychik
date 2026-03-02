using System;
using FluentAssertions;
using MusicService.Application.Common;
using Xunit;

namespace Tests.EFCoreTests
{
    public class DatabaseErrorDetectorTests
    {
        private sealed class SqlStateException : Exception
        {
            public string SqlState { get; }

            public SqlStateException(string sqlState)
            {
                SqlState = sqlState;
            }
        }

        private sealed class SqlNumberException : Exception
        {
            public int Number { get; }

            public SqlNumberException(int number)
            {
                Number = number;
            }
        }

        private sealed class SqliteErrorException : Exception
        {
            public int SqliteExtendedErrorCode { get; }

            public SqliteErrorException(int code)
            {
                SqliteExtendedErrorCode = code;
            }
        }

        [Fact]
        public void IsUniqueViolation_ShouldDetectPostgresSqlServerAndSqlite()
        {
            DatabaseErrorDetector.IsUniqueViolation(new SqlStateException("23505")).Should().BeTrue();
            DatabaseErrorDetector.IsUniqueViolation(new SqlNumberException(2601)).Should().BeTrue();
            DatabaseErrorDetector.IsUniqueViolation(new SqliteErrorException(1555)).Should().BeTrue();
            DatabaseErrorDetector.IsUniqueViolation(new Exception()).Should().BeFalse();
        }

        [Fact]
        public void IsForeignKeyViolation_ShouldDetectPostgresSqlServerAndSqlite()
        {
            DatabaseErrorDetector.IsForeignKeyViolation(new SqlStateException("23503")).Should().BeTrue();
            DatabaseErrorDetector.IsForeignKeyViolation(new SqlNumberException(547)).Should().BeTrue();
            DatabaseErrorDetector.IsForeignKeyViolation(new SqliteErrorException(787)).Should().BeTrue();
            DatabaseErrorDetector.IsForeignKeyViolation(new Exception()).Should().BeFalse();
        }

        [Fact]
        public void IsTransient_ShouldDetectPostgresAndSqlServer()
        {
            DatabaseErrorDetector.IsTransient(new SqlStateException("40001")).Should().BeTrue();
            DatabaseErrorDetector.IsTransient(new SqlStateException("40P01")).Should().BeTrue();
            DatabaseErrorDetector.IsTransient(new SqlNumberException(1205)).Should().BeTrue();
            DatabaseErrorDetector.IsTransient(new Exception()).Should().BeFalse();
        }
    }
}
