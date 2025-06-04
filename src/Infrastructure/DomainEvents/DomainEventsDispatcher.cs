using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Infrastructure.DomainEvents;

internal sealed class DomainEventsDispatcher(IServiceProvider serviceProvider) : IDomainEventsDispatcher
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeDictionary = new();
    private static readonly ConcurrentDictionary<Type, Type> WrapperTypeDictionary = new();

    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (IDomainEvent domainEvent in domainEvents)
        {
            using IServiceScope scope = serviceProvider.CreateScope();

            Type domainEventType = domainEvent.GetType();
            Type handlerType = HandlerTypeDictionary.GetOrAdd(
                domainEventType,
                et => typeof(IDomainEventHandler<>).MakeGenericType(et));

            IEnumerable<object?> handlers = scope.ServiceProvider.GetServices(handlerType);

            foreach (object? handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }
                // instead of using reflection directly, we use a wrapper to handle the type casting
                // and method invocation, which is more efficient and cleaner.
                /**
                * This is ILLEGAL in C# - can't use 'domainEventType' (a runtime variable) in generics!
                await (IDomainEventHandler<domainEventType>)handler.Handle(...);
                
                * Instead , we passed that domainEventType to HandlerWrapper
                    which it will create a handlerWrapper Dynamically based on the type of the domain event ( reflecion ).
                    
                    this trick works because the HandlerWrapper is a generic class, and we can create a specific instance of it
                    =>  typeof(HandlerWrapper<>).MakeGenericType(domainEventType)) 
                    => will create HanderWrapper<domainEventType>
                */

                var handlerWrapper = HandlerWrapper.Create(handler, domainEventType);

                await handlerWrapper.Handle(domainEvent, cancellationToken);
            }
        }
    }

    private abstract class HandlerWrapper
    {
        public abstract Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken);

        public static HandlerWrapper Create(object handler, Type domainEventType)
        {
            Type wrapperType = WrapperTypeDictionary.GetOrAdd(
                domainEventType,
                et => typeof(HandlerWrapper<>).MakeGenericType(et));

            return (HandlerWrapper)Activator.CreateInstance(wrapperType, handler);
        }
    }

    private sealed class HandlerWrapper<T>(object handler) : HandlerWrapper where T : IDomainEvent
    {
          // now we can safely cast the handler to IDomainEventHandler<T>
        // because an instance of HandlerWrapper<T> is created with the appropriate type T
        // so it can be used at compile time now 
        private readonly IDomainEventHandler<T> _handler = (IDomainEventHandler<T>)handler;

        public override async Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await _handler.Handle((T)domainEvent, cancellationToken);
        }
    }
}
