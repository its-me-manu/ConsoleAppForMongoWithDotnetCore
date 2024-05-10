using System;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace MongoPersistance
{
    class Program
    {
        static async Task Main(string[] args)
        {

            const string connectionUri = "";

            var settings = MongoClientSettings.FromConnectionString(connectionUri);

            // Set the ServerApi field of the settings object to set the version of the Stable API on the client
            settings.ServerApi = new ServerApi(ServerApiVersion.V1);

            // Create a new client and connect to the server
            var client = new MongoClient(settings);

            try
            {
                //Fetching the collection from database
                var dbCollection = client.GetDatabase("SamplePersistance").GetCollection<BsonDocument>("TestObjects");
                
                //Setting sample search condition
                var column = "Area";
                var value = "northern";

                //Normal filter builder
                var filter = Builders<BsonDocument>.Filter.Eq(column, value);
                var result = (await dbCollection.FindAsync(filter)).ToListAsync();

                //Fetching the documents by building the expression
                var fxvalue = "western";
                Expression<Func<BsonDocument, bool>> criteria = x => x[column].AsBsonValue.AsString.Contains(fxvalue);
                Expression<Func<BsonDocument, bool>> andCriteria = x => x[column].AsBsonValue.AsString.Contains(value);
                criteria = ExpressionVisitorOrBuilder(criteria, andCriteria, criteria.Parameters.FirstOrDefault());

                var expressionFilter = Builders<BsonDocument>.Filter.Where(criteria);
                var expressionResult= (await dbCollection.FindAsync(expressionFilter)).ToListAsync();


                Console.WriteLine(result.Result);
                Console.WriteLine(expressionResult.Result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        public static Expression<Func<T, bool>> ExpressionVisitorAndBuilder<T>(Expression<Func<T, bool>> expressionCriteria, Expression<Func<T, bool>> newExpression, ParameterExpression parameterExpression)
        {
            var visitor = new ReplaceExpressionVisitor(newExpression.Parameters[0], parameterExpression);
            var additionalCriteriaBody = visitor.Visit(newExpression.Body);
            var body = Expression.AndAlso(expressionCriteria.Body, additionalCriteriaBody);
            return Expression.Lambda<Func<T, bool>>(body, parameterExpression);
        }

        public static Expression<Func<T, bool>> ExpressionVisitorOrBuilder<T>(Expression<Func<T, bool>> expressionCriteria, Expression<Func<T, bool>> newExpression, ParameterExpression parameterExpression)
        {
            var visitor = new ReplaceExpressionVisitor(newExpression.Parameters[0], parameterExpression);
            var additionalCriteriaBody = visitor.Visit(newExpression.Body);
            var body = Expression.OrElse(expressionCriteria.Body, additionalCriteriaBody);
            return Expression.Lambda<Func<T, bool>>(body, parameterExpression);
        }
    }
}
