using System;
using System.Threading.Tasks;
using Volo.Abp.Domain.Services;
using PibesDelDestino.Destinations;

namespace PibesDelDestino.Notifications
{
    public interface INotificationManager : IDomainService
    {
        Task NotifyEventInCityAsync(string cityName, string eventName, string eventUrl);
        Task NotifyDestinationUpdateAsync(Destination destination, string changeDescription);
        Task NotifyNewCommentAsync(Guid destinationId, string destinationName, Guid commenterId);
    }
}