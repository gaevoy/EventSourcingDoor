using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace EventSourcingDoor.Examples.TodoApp
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        public static readonly List<StreamWriter> Listeners = new();

        [HttpGet]
        public async Task ListenToEvents()
        {
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";
            Response.ContentType = "text/event-stream";
            await using var listener = new StreamWriter(Response.Body);
            lock (Listeners)
                Listeners.Add(listener);
            await listener.WriteAsync("event: connected\ndata:\n\n");
            await listener.FlushAsync();

            try
            {
                await Task.Delay(Timeout.Infinite, HttpContext.RequestAborted);
            }
            catch (TaskCanceledException)
            {
            }

            lock (Listeners)
                Listeners.Remove(listener);
        }

        public static async Task BroadcastEvent(object evt)
        {
            List<StreamWriter> listeners;
            lock (Listeners)
                listeners = Listeners.ToList();
            await Task.WhenAll(listeners.Select(async listener =>
            {
                try
                {
                    var data = new {type = evt.GetType().Name, content = evt};
                    await listener.WriteAsync($"data: {JsonConvert.SerializeObject(data)}\n\n");
                    await listener.FlushAsync();
                }
                catch (ObjectDisposedException)
                {
                }
            }));
        }
    }
}