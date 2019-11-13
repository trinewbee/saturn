using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Nano.Mongo
{
    class MongoTable<T> : IMongoQueryable<T>
    {
        public IMongoDatabase Db { get; }
        public IMongoCollection<T> Collection { get; }

        IMongoQueryable<T> m_query;

        public MongoTable(IMongoDatabase db, string name)
        {
            Db = db;
            Collection = db.GetCollection<T>(name);
            m_query = Collection.AsQueryable();
        }

        public void Add(T value, InsertOneOptions options = null, CancellationToken cancellationToken = default) =>
            Collection.InsertOne(value, options, cancellationToken);

        public List<T> Select(FilterDefinition<T> filter, FindOptions options = null)
        {
            var rs = Collection.Find(filter, options);
            return rs.ToList();
        }

        public List<T> Select(Expression<Func<T, bool>> expr, FindOptions options = null)
        {
            var rs = Collection.Find(expr, options);
            return rs.ToList();
        }

        public T this[Expression<Func<T, bool>> expr] => Get(expr);

        public T Get(FilterDefinition<T> filter, FindOptions options = null)
        {
            var items = Select(filter, options);
            return _SelectSingle(items);
        }

        public T Get(Expression<Func<T, bool>> expr, FindOptions options = null)
        {
            var items = Select(expr, options);
            return _SelectSingle(items);
        }

        static T _SelectSingle(IList<T> items)
        {
            if (items.Count == 1)
                return items[0];
            else if (items.Count == 0)
                return default;
            else
                throw new Exception("Too many results");
        }

        public DeleteResult DeleteMany(FilterDefinition<T> filter, DeleteOptions options = null, CancellationToken cancellationToken = default) =>
            Collection.DeleteMany(filter, options, cancellationToken);

        public DeleteResult DeleteMany(Expression<Func<T, bool>> filter, DeleteOptions options = null, CancellationToken cancellationToken = default) =>
            Collection.DeleteMany(filter, options, cancellationToken);

        public void Clear() => Collection.DeleteMany(FilterDefinition<T>.Empty);

        #region IMongoQueryable

        Expression IQueryable.Expression => m_query.Expression;

        Type IQueryable.ElementType => m_query.ElementType;

        IQueryProvider IQueryable.Provider => m_query.Provider;

        QueryableExecutionModel IMongoQueryable.GetExecutionModel() => m_query.GetExecutionModel();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => m_query.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_query.GetEnumerator();

        IAsyncCursor<T> IAsyncCursorSource<T>.ToCursor(CancellationToken cancellationToken) =>
            m_query.ToCursor(cancellationToken);

        Task<IAsyncCursor<T>> IAsyncCursorSource<T>.ToCursorAsync(CancellationToken cancellationToken) =>
            m_query.ToCursorAsync(cancellationToken);

        #endregion
    }

    public static class MongoKit
    {
        public const string TestUrl = "mongodb://admin:vnxFddeSsMQc@123.155.154.203:12129";

        public static MongoClient Open(string url = TestUrl) => new MongoClient(url);

        public static List<string> ListDatabases(MongoClient client)
        {
            var names = new List<string>();
            var cursor = client.ListDatabases();            
            while (cursor.MoveNext())
            {
                var e = cursor.Current;
                foreach (var doc in e)                    
                    names.Add((string)doc["name"]);
            }
            return names;           
        }

        public static IMongoDatabase GetDatabase(MongoClient client, string name, MongoDatabaseSettings settings = null)
            => client.GetDatabase(name, settings);

        public static List<string> ListCollectionNames(IMongoDatabase db)
        {
            var names = new List<string>();
            var rs = db.ListCollectionNames();
            while (rs.MoveNext())
                names.AddRange(rs.Current);
            return names;
        }
    }
}
