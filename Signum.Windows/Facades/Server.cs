﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Services;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;
using System.Reflection;
using System.ServiceModel;
using System.Windows;
using System.ServiceModel.Security;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Windows
{
    public static class Server
    {
        static Func<IBaseServer> getServer;
        
        static IBaseServer current;

        public static event Action Connecting;

        static Server()
        {
            Connecting += () =>
            {
                ServerTypes = current.ServerTypes();
                NameToType = ServerTypes.ToDictionary(a => a.Value.CleanName, a => a.Key);
            };
        }

        public static void SetNewServerCallback(Func<IBaseServer> server)
        {
            getServer = server;
        }

        public static void Connect()
        {
            if (!Connected)
            {
                current = getServer();

                if (current == null)
                    throw new InvalidOperationException(Properties.Resources.AConnectionWithTheServerIsNecessaryToContinue);

                if (Connecting != null)
                    Connecting();                     
            }
        }

        public static bool Connected
        {
            get
            {
                return (current is ICommunicationObject) && ((ICommunicationObject)current).State != CommunicationState.Faulted;
            }
        }
       
        public static void Execute<S>(Action<S> action)
            where S : class
        {
        retry:
            Connect();

            S server = current as S;
            if (server == null)
                throw new InvalidOperationException("Server {0} does not implement {1}".Formato(server.GetType(), typeof(S)));
            
            try
            {
                using (HeavyProfiler.Log("WCFClient", "{0}".Formato(typeof(S).TypeName())))
                {
                    action(server);
                }
            }
            catch (MessageSecurityException e)
            {
                HandleSessionException(e);
                current = null;
                goto retry;
            }
        }

        public static R Return<S, R>(Func<S, R> function)
          where S : class
        {
        retry:
            Connect();

            S server = current as S;
            if (server == null)
                throw new InvalidOperationException("Server {0} does not implement {1}".Formato(current.GetType(), typeof(S)));

            try
            {
                using (HeavyProfiler.Log("WCFClient", "{0} --> {1}".Formato(typeof(S).TypeName(), typeof(R).TypeName())))
                {
                    return function(server);
                }
            }
            catch (MessageSecurityException e)
            {
                HandleSessionException(e);
                current = null;
                goto retry;
            }
        }

        static void HandleSessionException(MessageSecurityException e)
        {
            MessageBox.Show(Properties.Resources.SessionExpired, Properties.Resources.SessionExpired, MessageBoxButton.OK, MessageBoxImage.Hand);
        }

        public static bool Implements<T>()
        {
            Connect();
            return current is T;
        }

        public static T Save<T>(this T entidad) where T : IdentifiableEntity
        {
            return (T)Return((IBaseServer s)=>s.Save(entidad));
        }

        public static IdentifiableEntity Save(IdentifiableEntity entidad)
        {
            return Return((IBaseServer s)=>s.Save(entidad)); 
        }

        public static T Retrieve<T>(int id) where T : IdentifiableEntity
        {
            return (T)Return((IBaseServer s)=>s.Retrieve(typeof(T), id)); 
        }

        public static IdentifiableEntity Retrieve(Type type, int id)
        {
            return Return((IBaseServer s)=>s.Retrieve(type, id)); 
        }

        public static IdentifiableEntity Retrieve(Lite lite)
        {
            if (lite.UntypedEntityOrNull == null)
            {
                lite.SetEntity(Return((IBaseServer s)=>s.Retrieve(lite.RuntimeType, lite.Id))); 
            }
            return lite.UntypedEntityOrNull;
        }

        public static T Retrieve<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            if (lite.EntityOrNull == null)
            {
                lite.SetEntity((IdentifiableEntity)(IIdentifiable)Return((IBaseServer s)=>s.Retrieve(lite.RuntimeType, lite.Id))); 
            }
            return lite.EntityOrNull;
        }

        public static IdentifiableEntity RetrieveAndForget(Lite lite)
        {
            return Return((IBaseServer s)=>s.Retrieve(lite.RuntimeType, lite.Id)); 
        }

        public static T RetrieveAndForget<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
            return (T)(IIdentifiable)Return((IBaseServer s)=>s.Retrieve(lite.RuntimeType, lite.Id)); 
        }

        public static List<T> RetrieveAll<T>() where T : IdentifiableEntity
        {
            return Return((IBaseServer s)=>s.RetrieveAll(typeof(T)).Cast<T>().ToList<T>()); 
        }

        public static List<IdentifiableEntity> RetrieveAll(Type type)
        {
            return Return((IBaseServer s)=>s.RetrieveAll(type)); 
        }

        public static List<Lite> RetrieveAllLite(Type liteType, Implementations implementations)
        {
            return Return((IBaseServer s)=>s.RetrieveAllLite(liteType, implementations)); 
        }

        public static List<Lite<T>> RetrieveAllLite<T>(Implementations implementations) where T : class, IIdentifiable
        {
            return Return((IBaseServer s)=>s.RetrieveAllLite(typeof(T), implementations).Cast<Lite<T>>().ToList()); 
        }


        public static List<Lite> FindLiteLike(Type liteType, Implementations implementations, string subString, int count)
        {
            return Return((IBaseServer s)=>s.FindLiteLike(liteType, implementations, subString, count)); 
        }

        public static List<T> SaveList<T>(List<T> list)
            where T: IdentifiableEntity
        {
            return Return((IBaseServer s)=>s.SaveList(list.Cast<IdentifiableEntity>().ToList()).Cast<T>().ToList()); 
        }

        static Dictionary<Type, Dictionary<PropertyRoute, Implementations>> implementations = new Dictionary<Type, Dictionary<PropertyRoute, Implementations>>();

        public static Implementations FindImplementations(PropertyRoute propertyRoute)
        {
            var dic = implementations.GetOrCreate(propertyRoute.RootType, () =>
            {
                if (!Server.ServerTypes.ContainsKey(propertyRoute.RootType))
                    return null;

                return Server.Return((IBaseServer s) => s.FindAllImplementations(propertyRoute.Type));
            });

            return dic.TryGetC(propertyRoute);
        }

        public static object Convert(object obj, Type type)
        {
            if (obj == null) return null;

            Type objType = obj.GetType();

            if (type.IsAssignableFrom(objType))
                return obj;

            if (objType.IsLite() && type.IsAssignableFrom(((Lite)obj).RuntimeType))
            {
                Lite lite = (Lite)obj;
                return lite.UntypedEntityOrNull ?? RetrieveAndForget(lite);
            }
            
            if (type.IsLite())
            {
                Type liteType = Reflector.ExtractLite(type); 
                
                if(objType.IsLite())
                {
                    Lite lite = (Lite)obj;
                    if (liteType.IsAssignableFrom(lite.RuntimeType))
                    {
                        if (lite.UntypedEntityOrNull != null)
                            return Lite.Create(liteType, lite.UntypedEntityOrNull);
                        else
                            return Lite.Create(liteType, lite.Id, lite.RuntimeType, lite.ToStr); 
                    }
                }

                else if(liteType.IsAssignableFrom(objType))
                {
                    return Lite.Create(liteType, (IdentifiableEntity)obj);
                }
            }

            throw new InvalidCastException("Impossible to convert objet {0} from type {1} to type {2}".Formato(obj, objType, type));
        }

        public static bool CanConvert(object obj, Type type)
        {
            if (obj == null) 
                return true;

            Type objType = obj.GetType();

            if (objType == type)
                return true;

            if (objType.IsLite() && ((Lite)obj).RuntimeType == type)
            {
                return true;
            }

            Type liteType;
            if (type.IsLite() && (liteType = Reflector.ExtractLite(type)).IsAssignableFrom(objType))
            {
                return true;
            }

            return false;
        }

        public static Dictionary<Type, TypeDN> ServerTypes { get; private set; }
        public static Dictionary<string, Type> NameToType { get; private set; }

        public static Type TryGetType(string cleanName)
        {
            return NameToType.TryGetC(cleanName);
        }

        public static Type GetType(string cleanName)
        {
            return NameToType.GetOrThrow(cleanName, "Type {0} not found in the Server");
        }

        public static string GetCleanName(Type type)
        {
            return ServerTypes[type].CleanName;
        }

        public static Lite ParseLite(Type liteType, string liteKey)
        {
            return Lite.ParseLite(liteType, liteKey, TryGetType);
        }

        public static string TryParseLite(Type liteType, string liteKey, out Lite result)
        {
            return Lite.TryParseLite(liteType, liteKey, TryGetType, out result);
        }

        public static Lite<T> FillToStr<T>(this Lite<T> lite) where T : class, IIdentifiable
        {
           lite.ToStr = Return((IBaseServer s) => s.GetToStr(lite.RuntimeType, lite.Id));

           return lite;
        }

        public static Lite FillToStr(Lite lite)
        {
            lite.ToStr = Return((IBaseServer s) => s.GetToStr(lite.RuntimeType, lite.Id));

            return lite;
        }
    }
}
