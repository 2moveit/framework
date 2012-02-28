﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using Signum.Engine.Properties;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Data.Common;

namespace Signum.Engine
{
    public enum Spacing
    {
        Simple,
        Double,
        Triple
    }

    public abstract class SqlPreCommand
    {
        public abstract IEnumerable<SqlPreCommandSimple> Leaves();

        protected internal abstract void GenerateScript(StringBuilder sb);

        protected internal abstract void GenerateParameters(List<DbParameter> list);

        protected internal abstract int NumParameters { get; }

        /// <summary>
        /// For debugging purposes
        /// </summary>
        public string PlainSql()
        {
            StringBuilder sb = new StringBuilder();
            this.PlainSql(sb);
            return sb.ToString(); 
        }

        protected internal abstract void PlainSql(StringBuilder sb);

        public abstract SqlPreCommandSimple ToSimple();

        public override string ToString()
        {
            return this.PlainSql();
        }

        public static SqlPreCommand Combine(Spacing spacing, params SqlPreCommand[] sentences)
        {
            if (sentences.Contains(null))
                sentences = sentences.NotNull().ToArray();

            if (sentences.Length == 0)
                return null;

            if (sentences.Length == 1)
                return sentences[0];

            return new SqlPreCommandConcat(spacing, sentences);
        }

        public abstract SqlPreCommand Clone(); 
    }

    public static class SqlPreCommandExtensions
    {
        public static SqlPreCommand Combine(this IEnumerable<SqlPreCommand> preCommands, Spacing spacing)
        {
            return SqlPreCommand.Combine(spacing, preCommands.ToArray());
        }

        public static void OpenSqlFile(this SqlPreCommand command)
        {
            OpenSqlFile(command, "Sync {0:dd-MM-yyyy}.sql".Formato(DateTime.Now));
        }

        public static void OpenSqlFileRetry(this SqlPreCommand command)
        {
            command.OpenSqlFile();
            Console.WriteLine("Open again?");
            string val = Console.ReadLine();
            if (!val.StartsWith("y") && !val.StartsWith("Y"))
                return;

            command.OpenSqlFile();
        }

        public static void OpenSqlFile(this SqlPreCommand command, string fileName)
        {
            string content = command.PlainSql(); 

            File.WriteAllText(fileName, content, Encoding.GetEncoding(1252));

            Thread.Sleep(1000);

            Process.Start(fileName); 
        }
    }

    public class SqlPreCommandSimple : SqlPreCommand
    {
        public string Sql { get; private set; }
        public List<DbParameter> Parameters { get; private set; }

        public SqlPreCommandSimple(string sql)
        {
            this.Sql = sql;
        }

        public SqlPreCommandSimple(string sql, List<DbParameter> parameters)
        {
            this.Sql = sql;
            this.Parameters = parameters;
        }

        public override IEnumerable<SqlPreCommandSimple> Leaves()
        {
            yield return this;
        }

        protected internal override void GenerateScript(StringBuilder sb)
        {
            sb.Append(Sql);
        }

        protected internal override void GenerateParameters(List<DbParameter> list)
        {
            if (Parameters != null)
                list.AddRange(Parameters);
        }

        public override SqlPreCommandSimple ToSimple()
        {
            return this;
        }

        protected internal override int NumParameters
        {
            get { return Parameters.TryCS(p => p.Count) ?? 0; }
        }

        static readonly Regex regex = new Regex(@"@[_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}][_\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nl}\p{Nd}]*");
      
        static string Encode(object value)
        {
            if (value == null || value == DBNull.Value)
                return "NULL";

            if (value is string)
                return "\'" + ((string)value).Replace("'", "''") + "'";

            if (value is DateTime)
                return "convert(datetime, '{0:s}', 126)".Formato(value);

            if (value is bool)
                return (((bool)value) ? 1 : 0).ToString();

            return value.ToString();
        }

        protected internal override void PlainSql(StringBuilder sb)
        {
            if (Parameters.IsNullOrEmpty())
                sb.Append(Sql);
            else
            {

                var dic = Parameters.ToDictionary(a => a.ParameterName, a => Encode(a.Value));

                sb.Append(regex.Replace(Sql, m => dic.TryGetC(m.Value) ?? m.Value));
            }
        }

        public override SqlPreCommand Clone()
        {
            return new SqlPreCommandSimple(Sql, Parameters == null ? null : Parameters
                .Select(p => Connector.Current.CloneParameter(p))
                .ToList());
        }

        public SqlPreCommandSimple AddComment(string comment)
        {
            if (comment.HasText())
            {
                int index = Sql.IndexOf("\r\n");
                if (index == -1)
                    Sql = Sql + " -- " + comment;
                else
                    Sql = Sql.Insert(index, " -- " + comment);
            }

            return this;
        }
    }

    public class SqlPreCommandConcat : SqlPreCommand
    {
        public Spacing Spacing { get; private set; }
        public SqlPreCommand[] Commands { get; private set; }

        internal SqlPreCommandConcat(Spacing spacing, SqlPreCommand[] commands)
        {
            this.Spacing = spacing;
            this.Commands = commands;
        }

        public override IEnumerable<SqlPreCommandSimple> Leaves()
        {
            return Commands.SelectMany(c => c.Leaves());
        }

        protected internal override void GenerateScript(StringBuilder sb)
        {
            string sep = separators[Spacing];
            bool borrar = false;
            foreach (SqlPreCommand com in Commands)
            {
                com.GenerateScript(sb);
                sb.Append(sep);
                borrar = true;
            }

            if (borrar) sb.Remove(sb.Length - sep.Length, sep.Length);
        }

        protected internal override void GenerateParameters(List<DbParameter> list)
        {
            foreach (SqlPreCommand com in Commands)
                com.GenerateParameters(list);
        }

        public override SqlPreCommandSimple ToSimple()
        {
            StringBuilder sb = new StringBuilder();
            GenerateScript(sb);

            List<DbParameter> parameters = new List<DbParameter>();
            GenerateParameters(parameters);

            return new SqlPreCommandSimple(sb.ToString(), parameters);
        }

        static Dictionary<Spacing, string> separators = new Dictionary<Spacing, string>()
        {
            {Spacing.Simple, ";\r\n"},
            {Spacing.Double, ";\r\n\r\n"},
            {Spacing.Triple, ";\r\n\r\n\r\n"},
        };

        protected internal override int NumParameters
        {
            get { return Commands.Sum(c => c.NumParameters); }
        }

        protected internal override void PlainSql(StringBuilder sb)
        {
            string sep = separators[Spacing];
            bool borrar = false;
            foreach (SqlPreCommand com in Commands)
            {
                com.PlainSql(sb);
                sb.Append(sep);
                borrar = true;
            }

            if (borrar) sb.Remove(sb.Length - sep.Length, sep.Length);
        }

        public override SqlPreCommand Clone()
        {
            return new SqlPreCommandConcat(Spacing, Commands.Select(c => c.Clone()).ToArray());  
        }
    }

}
