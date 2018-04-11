using System;
using System.Collections.Generic;
using System.Linq.Expressions;

/// <summary>
///  Quick and dirty sample showing how lambda expressions can be used to create a very simple rules processor
/// </summary>

namespace Rules
{
    class Program
    {
        public class User
        {
            public string Name
            {
                get;
                set;
            }
            public int Age
            {
                get;
                set;
            }
        }

        public class simpleString
        {
            public string searchValue
            {
                get;
                set;
            }
        }

        static Expression BuildExpr<T>(Rule r, ParameterExpression param)
        {
            var left = MemberExpression.Property(param, r.MemberName);
            var tProp = typeof(T).GetProperty(r.MemberName).PropertyType;
            ExpressionType tBinary;
            // is the operator a known .NET operator?
            if (ExpressionType.TryParse(r.Operator, out tBinary))
            {
                var right = Expression.Constant(Convert.ChangeType(r.TargetValue, tProp));
                // use a binary operation, e.g. 'Equal' -> 'u.Age == 15'
                return Expression.MakeBinary(tBinary, left, right);
            }
            else
            {
                var method = tProp.GetMethod(r.Operator);
                var tParam = method.GetParameters()[0].ParameterType;
                var right = Expression.Constant(Convert.ChangeType(r.TargetValue, tParam));
                // use a method call, e.g. 'Contains' -> 'u.Tags.Contains(some_tag)'
                return Expression.Call(left, method, right);
            }
        }

        public static void Main()
        {
            // Numeric comparison
            var user1 = new User{Age = 13, Name = "Adam"};
            var user2 = new User{Age = 33, Name = "John"};
            var user3 = new User{Age = 53, Name = "DBag"};

            // Equal, NotEqual, GreaterThanOrEqual, GreaterThan, LessThan, LessThanOrEqual
            var rule = new Rule("Age", "LessThanOrEqual", "33");
            Func<User, bool> compiledRule = CompileUserRule<User>(rule);

            Console.WriteLine("Rule: " + rule.MemberName + " " + rule.Operator + " " + rule.TargetValue);
            Console.WriteLine("Evaluating Rule:");
            Console.WriteLine("\tuser1: Name: " + user1.Name + ", Age: " + user1.Age + " : " + compiledRule(user1));
            Console.WriteLine("\tuser2: Name: " + user2.Name + ", Age: " + user2.Age + " : " + compiledRule(user2));
            Console.WriteLine("\tuser3: Name: " + user3.Name + ", Age: " + user3.Age + " : " + compiledRule(user3));

            // Search within a string
            var mystring = new simpleString { searchValue = "I got ants!" };
            rule = new Rule("searchValue", "Contains", "ants");
            Func<simpleString, bool> compiledStringRule = CompileStringRule<simpleString>(rule);
            compiledStringRule = CompileStringRule<simpleString>(rule);

            Console.WriteLine("\nRule: " + rule.MemberName + " " + rule.Operator + " " + rule.TargetValue);
            Console.WriteLine("Evaluating Rule:");
            Console.WriteLine("\tsearchValue: \"" + mystring.searchValue + "\" : " + compiledStringRule(mystring));
            mystring.searchValue = "If you see something, say something.";
            Console.WriteLine("\tsearchValue: \"" + mystring.searchValue + "\" : " + compiledStringRule(mystring));

            // Dynamic rule creation and execution
            Console.WriteLine("\nDynamically constructed rule with simple math expression:");
            System.Linq.Expressions.BinaryExpression binaryExpression =
                System.Linq.Expressions.Expression.MakeBinary(
                    System.Linq.Expressions.ExpressionType.Divide,
                    System.Linq.Expressions.Expression.Constant(1000),
                    System.Linq.Expressions.Expression.Constant(20)
                );
            Console.WriteLine("\tRule: " + binaryExpression.ToString());
            // Compile the expression.  
            var compiledExpression = Expression.Lambda<Func<int>>(binaryExpression).Compile();
            // Execute the expression.  
            Console.WriteLine("\tResult of rule execution: " + compiledExpression());

            Console.WriteLine("\nHit any key to exit");
            Console.ReadKey();

        }

        public static Func<T, bool> CompileUserRule<T>(Rule r)
        {
            var paramUser = Expression.Parameter(typeof(User));
            Expression expr = BuildExpr<T>(r, paramUser);
            // build a lambda function User->bool and compile it
            return Expression.Lambda<Func<T, bool>>(expr, paramUser).Compile();
        }
        public static Func<T, bool> CompileStringRule<T>(Rule r)
        {
            var paramString = Expression.Parameter(typeof(simpleString));
            Expression expr = BuildExpr<T>(r, paramString);
            // build a lambda function User->bool and compile it
            return Expression.Lambda<Func<T, bool>>(expr, paramString).Compile();
        }

        public class Rule
        {
            public string MemberName
            {
                get;
                set;
            }

            public string Operator
            {
                get;
                set;
            }

            public string TargetValue
            {
                get;
                set;
            }

            public Rule(string MemberName, string Operator, string TargetValue)
            {
                this.MemberName = MemberName;
                this.Operator = Operator;
                this.TargetValue = TargetValue;
            }
        }
    }
}
