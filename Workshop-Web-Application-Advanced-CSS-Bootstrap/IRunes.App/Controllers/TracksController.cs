﻿using System.Linq;
using IRunes.Models;
using IRunes.Services;
using SIS.MvcFramework;
using SIS.HTTP.Requests;
using IRunes.App.ViewModels;
using SIS.MvcFramework.Result;
using SIS.MvcFramework.Mapping;
using SIS.MvcFramework.Attributes.Http;
using SIS.MvcFramework.Attributes.Security;

namespace IRunes.App.Controllers
{
    public class TracksController : Controller
    {
        private readonly ITrackService trackService;
        private readonly IAlbumService albumService;

        public TracksController(ITrackService trackService, IAlbumService albumService)
        {
            this.trackService = trackService;
            this.albumService = albumService;
        }

        [Authorize]
        public ActionResult Create(IHttpRequest httpRequest)
        {
            string albumId = httpRequest.QueryData["albumId"].FirstOrDefault();

            return this.View(new TrackCreateViewModel { AlbumId = albumId });
        }

        [Authorize]
        [HttpPost(ActionName = "Create")]
        public ActionResult CreateConfirm()
        {
            string albumId = this.Request.QueryData["albumId"].FirstOrDefault();
            string name = this.Request.FormData["name"].FirstOrDefault();
            string link = this.Request.FormData["link"].FirstOrDefault();
            string price = this.Request.FormData["price"].FirstOrDefault();

            Track trackForDb = new Track
            {
                Name = name,
                Link = link,
                Price = decimal.Parse(price)
            };

            if (!this.albumService.AddTrackToAlbum(albumId, trackForDb))
            {
                return this.Redirect("Albums/All");
            }

            return this.Redirect($"/Albums/Details?id={albumId}");
        }

        [Authorize]
        public ActionResult Details()
        {
            string albumId = this.Request.QueryData["albumId"].FirstOrDefault();
            string trackId = this.Request.QueryData["trackId"].FirstOrDefault();

            Track trackFromDb = this.trackService.GetTrackById(trackId);

            if (trackFromDb == null)
            {
                return this.Redirect($"/Albums/Details?id={albumId}");
            }

            TrackDetailsViewModel trackDetailsViewModel = ModelMapper.ProjectTo<TrackDetailsViewModel>(trackFromDb);
            trackDetailsViewModel.AlbumId = albumId;

            return this.View(trackDetailsViewModel);
        }
    }
}
