using System.Reflection;
using MessagePack;
using MessagePack.Formatters;

namespace Pkl.InternalMsgApi;

/// <summary>
/// Implements a MessagePack formatter that leaves out object properties that have their default
/// value, as well as properties marked as ignored.
/// </summary>
/// <remarks>
/// Workaround for https://github.com/MessagePack-CSharp/MessagePack-CSharp/issues/678
/// </remarks>
/// <typeparam name="T">The type to format.</typeparam>
public class NoDefaultsFormatter<T> : IMessagePackFormatter<T>
{
	/// <summary>
	/// Serializes the object.
	/// </summary>
	public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
	{
		var dict = new Dictionary<string, object?>();
		foreach (var property in typeof(T).GetProperties())
		{
			if (property.GetCustomAttribute<IgnoreMemberAttribute>() is not null)
			{
				continue;
			}

			var propValue = property.GetValue(value);
			if (object.Equals(propValue, GetDefault(property.PropertyType)))
			{
				continue;
			}

			var name = property.Name;

			var keyAttribute = property.GetCustomAttribute<KeyAttribute>();
			if (keyAttribute is not null && keyAttribute.StringKey is not null)
			{
				name = keyAttribute.StringKey;
			}

			dict[name] = propValue;
		}
		
		options.Resolver.GetFormatterWithVerify<Dictionary<string, object?>>().Serialize(ref writer, dict, options);
	}

	/// <summary>
	/// Deserializes the object.
	/// </summary>
	public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
	{
		throw new NotImplementedException();
	}

	private static object? GetDefault(Type type)
	{
		if(type.IsValueType)
		{
			return Activator.CreateInstance(type);
		}

		return null;
	}
}