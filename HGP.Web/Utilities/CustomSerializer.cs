using HGP.Web.Models;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HGP.Web.Utilities
{
    public class CustomSerializer : IBsonSerializer
    {
        object IBsonSerializer.Deserialize(BsonReader bsonReader, Type nominalType, IBsonSerializationOptions options)
        {
            if (bsonReader.CurrentBsonType == MongoDB.Bson.BsonType.Double)
                return bsonReader.ReadDouble().ToString();
            else
                return bsonReader.ReadString();
        }

        object IBsonSerializer.Deserialize(BsonReader bsonReader, Type nominalType, Type actualType, IBsonSerializationOptions options)
        {
            if (bsonReader.CurrentBsonType == MongoDB.Bson.BsonType.Double)
                return bsonReader.ReadDouble().ToString();
            else
                return bsonReader.ReadString();
        }

        IBsonSerializationOptions IBsonSerializer.GetDefaultSerializationOptions()
        {
            return null;
        }

        void IBsonSerializer.Serialize(BsonWriter bsonWriter, Type nominalType, object value, IBsonSerializationOptions options)
        {
            bsonWriter.WriteString(value as string);
        }
    }
}
