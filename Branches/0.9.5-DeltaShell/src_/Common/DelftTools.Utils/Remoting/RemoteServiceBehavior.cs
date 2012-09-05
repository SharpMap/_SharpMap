using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace DelftTools.Utils.Remoting
{
    public class RemoteServiceBehavior : IServiceBehavior
    {
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher channelDispatcher in serviceHostBase.ChannelDispatchers)
            {
                channelDispatcher.ErrorHandlers.Add(new RemoteErrorHandler());
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }
    }

    public class RemoteErrorHandler : IErrorHandler
    {
        public bool HandleError(Exception error)
        {
            Console.WriteLine("This error occurred somewhere: {0}", error.Message);
            return true;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {   
            var exceptionPrefix = "[" + error.GetType().Name + "]: ";
            var message = exceptionPrefix + error.Message;

            fault = Message.CreateMessage(version, new FaultException<string>(message, new FaultReason(message)).CreateMessageFault(), "");
        }
    }
}
