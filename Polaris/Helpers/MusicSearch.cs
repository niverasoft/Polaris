using NiveraLib;
using NiveraLib.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using YoutubeExplode;
using YoutubeSearch;

using SpotifyAPI.Web;

using YoutubeExplode.Search;
using YoutubeExplode.Playlists;

using Polaris.Config;
using YoutubeExplode.Videos;

namespace Polaris.Helpers.Music
{
    public interface IResult
    {
        IMusicTrack SelectedTrack { get; }

        IList<IMusicTrack> OtherTracks { get; }
    }

    public interface IMusicTrack
    {
        string Url { get; }
        string Title { get; }
        string Author { get; }
        string AuthorUrl { get; }
        string Thumbnail { get; }

        TimeSpan Duration { get; }
    }

    public class TrackSearchResult : IResult
    {
        public IMusicTrack SelectedTrack { get; }
        public IList<IMusicTrack> OtherTracks => null;

        public TrackSearchResult(IMusicTrack track)
        {
            SelectedTrack = track;
        }
    }

    public class PlaylistSearchResult : IResult
    {
        public string Name { get; }
        public string Author { get; }
        public string AuthorUrl { get; }
        public string ThumbnailUrl { get; }

        public IMusicTrack SelectedTrack { get; }
        public IList<IMusicTrack> OtherTracks { get; }

        public PlaylistSearchResult(
            string name, 
            string author, 
            string authorUrl,
            string thumbnailUrl,

            IMusicTrack selectedTrack,
            IList<IMusicTrack> otherTracks)
        {
            Name = name;
            Author = author;
            AuthorUrl = authorUrl;
            ThumbnailUrl = thumbnailUrl;
            SelectedTrack = selectedTrack;

            otherTracks.RemoveAt(0);

            OtherTracks = otherTracks;
        }
    }

    public class QuerySearchResult : IResult
    {
        public QuerySearchResult(IList<IMusicTrack> queriedTracks)
        {
            SelectedTrack = queriedTracks.First();

            queriedTracks.RemoveAt(0);

            OtherTracks = queriedTracks;
        }

        public IMusicTrack SelectedTrack { get; }
        public IList<IMusicTrack> OtherTracks { get; }
    }

    public class ErrorSearchResult : IResult
    {
        public ErrorSearchResult(string reason)
        {
            Reason = reason;
        }

        public IMusicTrack SelectedTrack => null;
        public IList<IMusicTrack> OtherTracks => null;

        public string Reason { get; }
    }

    public class YouTubeMusicTrack : IMusicTrack
    {
        public string Url { get; }
        public string Title { get; }
        public string Author { get; }
        public string AuthorUrl { get; }
        public string Thumbnail { get; }

        public TimeSpan Duration { get; }

        internal YouTubeMusicTrack(
            string url,
            string title,
            string author,
            string authorUrl,
            string thumbnailUrl,

            TimeSpan duration)
        {
            Url = url;
            Title = title;
            Author = author;
            AuthorUrl = authorUrl;
            Thumbnail = thumbnailUrl;

            Duration = duration;
        }
    }

    public class Spotify
    {
        private LogId _logId;
        private SpotifyClient _client;

        public Spotify()
        {
            _logId = LogIdGenerator.GenerateId("spotifyClient");

            Task.Run(() =>
            {
                var clientSecret = GlobalConfig.Instance.SpotifyClientSecret;
                var clientId = GlobalConfig.Instance.SpotifyClientId;

                if (string.IsNullOrEmpty(clientSecret))
                {
                    Log.SendWarn("You have not set the client secret!", _logId);

                    return;
                }

                if (string.IsNullOrEmpty(clientId))
                {
                    Log.SendWarn("You have not set the client ID!", _logId);

                    return;
                }

                var config = SpotifyClientConfig.CreateDefault()
                                .WithAuthenticator(new ClientCredentialsAuthenticator(clientId, clientSecret));

                _client = new SpotifyClient(config);

                IsValid = true;
            });
        }

        public bool IsValid { get; set; }

        public async Task<IResult> Search(string link)
        {
            try
            {
                Log.SendTrace($"Converting link: {link}");

                if (link.Contains("spotify.com/playlist/"))
                {
                    Log.SendTrace($"Identified playlist!");

                    link = StringHelpers.RemoveBeforeIndex(link, link.LastIndexOf('/'));

                    int idIndex = link.LastIndexOf('?');

                    if (idIndex != -1)
                        link = link.Remove(idIndex, link.Length - idIndex);

                    if (string.IsNullOrEmpty(link))
                        return new ErrorSearchResult("That link is invalid.");

                    var playlist = await _client.Playlists.Get(link);

                    if (playlist == null)
                        return new ErrorSearchResult("Failed to fetch that playlist.");

                    Log.SendTrace($"Playlist received.");

                    var tracks = playlist.Tracks.Items;
                    var convertedTracks = new List<IMusicTrack>();

                    Log.SendTrace("Converting ..");

                    foreach (var trackObj in tracks)
                    {
                        if (!(trackObj.Track is FullTrack spotifyTrack))
                            continue;

                        var convertedTrack = await MusicSearch.SearchOnYouTubeQuery($"{spotifyTrack.Artists.First().Name} {spotifyTrack.Name}", 1);

                        if (convertedTrack != null)
                        {
                            if (convertedTrack is ErrorSearchResult err)
                            {
                                Log.SendWarn(err.Reason);

                                continue;
                            }

                            if (convertedTrack.SelectedTrack == null)
                                continue;

                            Log.SendTrace($"Track converted: {convertedTrack.SelectedTrack.Title}");

                            convertedTracks.Add(convertedTrack.SelectedTrack);
                        }
                        else
                            Log.SendWarn($"Failed to find track: {spotifyTrack.Name}");
                    }

                    return new PlaylistSearchResult(playlist.Name, playlist.Owner.DisplayName, playlist.Owner.Uri, playlist.Images.First().Url, convertedTracks.First(), convertedTracks);
                }

                if (link.Contains("spotify.com/album/"))
                {
                    Log.SendTrace($"Identified album!");

                    link = StringHelpers.RemoveBeforeIndex(link, link.LastIndexOf('/'));

                    int idIndex = link.LastIndexOf('?');

                    if (idIndex != -1)
                        link = link.Remove(idIndex, link.Length - idIndex);

                    if (string.IsNullOrEmpty(link))
                        return new ErrorSearchResult("That link is invalid.");

                    var playlist = await _client.Albums.Get(link);

                    if (playlist == null)
                        return new ErrorSearchResult("Failed to fetch that playlist.");

                    Log.SendTrace($"Album received.");

                    var tracks = playlist.Tracks.Items;
                    var convertedTracks = new List<IMusicTrack>();

                    foreach (var trackObj in tracks)
                    {
                        var convertedTrack = await MusicSearch.SearchOnYouTubeQuery($"{trackObj.Artists.First().Name} {trackObj.Name}", 1);

                        if (convertedTrack != null)
                        {
                            if (convertedTrack is ErrorSearchResult err)
                            {
                                Log.SendWarn(err.Reason);

                                continue;
                            }

                            if (convertedTrack.SelectedTrack == null)
                                continue;

                            Log.SendTrace($"Track converted: {convertedTrack.SelectedTrack.Title}");

                            convertedTracks.Add(convertedTrack.SelectedTrack);
                        }
                        else
                            Log.SendWarn($"Failed to find track: {trackObj.Name}");
                    }

                    return new PlaylistSearchResult(playlist.Name, playlist.Artists.First().Name, playlist.Artists.First().Uri, playlist.Images.First().Url, convertedTracks.First(), convertedTracks);
                }

                if (link.Contains("spotify.com/track/"))
                {
                    Log.SendTrace($"Identified track!");

                    link = StringHelpers.RemoveBeforeIndex(link, link.LastIndexOf('/'));

                    int idIndex = link.LastIndexOf('?');

                    if (idIndex != -1)
                        link = link.Remove(idIndex, link.Length - idIndex);

                    if (string.IsNullOrEmpty(link))
                        return new ErrorSearchResult("That link is invalid.");

                    var track = await _client.Tracks.Get(link);

                    if (track == null)
                        return new ErrorSearchResult("Failed to fetch that track.");

                    Log.SendTrace($"Track received.");

                    var convertedTrack = await MusicSearch.SearchOnYouTubeQuery(track.Name, 1);

                    if (convertedTrack is ErrorSearchResult error)
                        return error;

                    Log.SendTrace($"Converted track: {convertedTrack.SelectedTrack.Title}");

                    return convertedTrack;
                }

                return new ErrorSearchResult("Invalid link. It must be an absolute path that contains the ID of the track/playlist you want to play.");
            }
            catch (Exception ex)
            {
                return new ErrorSearchResult(ex.Message);
            }
        }
    }

    public enum SearchType
    {
        YouTubeQuery,
        YouTubeLink,
        SpotifyLink,
    }

    public static class MusicSearch
    {
        private static YoutubeClient _search;
        private static Spotify _spotify;
        private static LogId _logId;

        static MusicSearch()
        {
            _logId = LogIdGenerator.GenerateId("musicSearchCore");

            _search = new YoutubeClient();
            _spotify = new Spotify();
        }

        public static void Load()
        {

        }

        public static async Task<IResult> Search(string query)
        {
            return await Search(DetermineType(query), query);
        }

        public static async Task<IResult> Search(SearchType type, string query)
        {
            if (type == SearchType.SpotifyLink)
                return await SearchOnSpotify(query);

            if (type == SearchType.YouTubeLink)
                return await SearchOnYouTubeLink(query);

            if (type == SearchType.YouTubeQuery)
                return await SearchOnYouTubeQuery(query);

            return new ErrorSearchResult("Failed to match a known search type.");
        }

        public static async Task<IResult> SearchOnYouTubeQuery(string query, int limit = -1)
        {
            try
            {
                List<IMusicTrack> results = new List<IMusicTrack>();

                await foreach (var result in _search.Search.GetResultsAsync(query))
                {
                    switch (result)
                    {
                        case VideoSearchResult videoSearch:
                            {
                                if (limit != -1)
                                {
                                    if (results.Count >= limit)
                                    {
                                        return new QuerySearchResult(results);
                                    }
                                }

                                results.Add(YtExplodeToMusicTrack(videoSearch));

                                break;
                            }

                        case YoutubeExplode.Search.PlaylistSearchResult _:
                            {
                                break;
                            }

                        case ChannelSearchResult _:
                            {
                                break;
                            }
                    }
                }

                Log.SendTrace($"Yt-Query: Found {results.Count} results for {query}");

                if (results.Count > 0)
                    return new QuerySearchResult(results);
                else
                    return new ErrorSearchResult("No results found for your query.");
            }
            catch (Exception ex)
            {
                return new ErrorSearchResult(ex.Message);
            }
        }

        public static async Task<IResult> SearchOnYouTubeLink(string link)
        {
            try
            {
                var searchType = GetFilter(link);

                Log.SendTrace($"Yt-Link: Using search filter: {searchType}");

                if (searchType == SearchFilter.Playlist)
                {
                    Log.SendTrace($"Identified Playlist, parsing ID");

                    var playlistId = PlaylistId.TryParse(link);

                    if (!playlistId.HasValue)
                        return new ErrorSearchResult("Failed to extract the playlist ID.");

                    Log.SendTrace($"ID parsed: {playlistId.Value.Value}");
                    Log.SendTrace($"Retrieving playlist ..");

                    var playlist = await _search.Playlists.GetAsync(playlistId.Value);

                    if (playlist == null)
                        return new ErrorSearchResult("Failed to retrieve specified playlist.");

                    Log.SendTrace($"Playlist retrieved, converting tracks.");

                    List<PlaylistVideo> plVideos = new List<PlaylistVideo>();
                    List<IMusicTrack> converted = new List<IMusicTrack>();

                    Log.SendTrace($"Retrieiving track list ..");

                    await foreach (var res in _search.Playlists.GetVideosAsync(playlistId.Value))
                    {
                        Log.SendTrace($"Adding: {res.Title}");

                        plVideos.Add(res);
                    }

                    foreach (var video in plVideos)
                    {
                        Log.SendTrace($"Converting: {video.Title}");

                        var ytVid = await _search.Videos.GetAsync(video.Id);

                        converted.Add(YtExplodeToMusicTrack(ytVid));

                        Log.SendTrace($"Converted: {video.Title}");
                    }

                    Log.SendTrace($"Loaded {converted.Count} track(s) from playlist {playlist.Title}");

                    return new PlaylistSearchResult(
                        playlist.Title,
                        playlist.Author.ChannelTitle,
                        playlist.Author.ChannelUrl,
                        playlist.Thumbnails.FirstOrDefault()?.Url ?? null,
                        converted.First(),
                        converted);

                }
                else if (searchType == SearchFilter.Video)
                {
                    var videoId = VideoId.TryParse(link);

                    if (!videoId.HasValue)
                        return new ErrorSearchResult("Failed to extract the video ID.");

                    var track = await _search.Videos.GetAsync(videoId.Value);

                    if (track == null)
                        return new ErrorSearchResult("Failed to retrieve specified track.");

                    var converted = YtExplodeToMusicTrack(track);

                    return new TrackSearchResult(converted);
                }
                else
                {

                    await foreach (var result in _search.Search.GetResultBatchesAsync(link, searchType))
                    {
                        foreach (var item in result.Items)
                        {
                            switch (item)
                            {
                                case VideoSearchResult videoSearch:
                                    {
                                        return YtExplodeToResult(videoSearch);
                                    }

                                case YoutubeExplode.Search.PlaylistSearchResult playlistSearch:
                                    {
                                        List<PlaylistVideo> plVideos = new List<PlaylistVideo>();
                                        List<IMusicTrack> converted = new List<IMusicTrack>();

                                        await foreach (var res in _search.Playlists.GetVideosAsync(PlaylistId.Parse(playlistSearch.Url)))
                                        {
                                            plVideos.Add(res);
                                        }

                                        foreach (var video in plVideos)
                                        {
                                            var ytVid = await _search.Videos.GetAsync(video.Id);

                                            converted.Add(YtExplodeToMusicTrack(ytVid));

                                            Log.SendInfo($"Track converted.", _logId);
                                        }

                                        return new PlaylistSearchResult(
                                            playlistSearch.Title,
                                            playlistSearch.Author.ChannelTitle,
                                            playlistSearch.Author.ChannelUrl,
                                            playlistSearch.Thumbnails.FirstOrDefault()?.Url ?? null,
                                            converted.First(),
                                            converted);
                                    }

                                case ChannelSearchResult _:
                                    {
                                        return new ErrorSearchResult("Search resulted in a channel.");
                                    }
                            }
                        }
                    }
                }

                return new ErrorSearchResult("Search failed; unknown.");
            }
            catch (Exception ex)
            {
                return new ErrorSearchResult(ex.Message);
            }
        }

        private static IResult YtExplodeToResult(VideoSearchResult videoSearch)
        {
            return new TrackSearchResult(new YouTubeMusicTrack(
                videoSearch.Url,
                videoSearch.Title,
                videoSearch.Author.ChannelTitle,
                videoSearch.Author.ChannelUrl,
                videoSearch.Thumbnails.FirstOrDefault()?.Url ?? null,
                videoSearch.Duration.GetValueOrDefault()
            ));
        }

        private static IMusicTrack YtExplodeToMusicTrack(VideoSearchResult videoSearch)
        {
            return new YouTubeMusicTrack(
                videoSearch.Url,
                videoSearch.Title,
                videoSearch.Author.ChannelTitle,
                videoSearch.Author.ChannelUrl,
                videoSearch.Thumbnails.FirstOrDefault()?.Url ?? null,
                videoSearch.Duration.GetValueOrDefault()
            );
        }

        private static IMusicTrack YtExplodeToMusicTrack(YoutubeExplode.Videos.Video videoSearch)
        {
            return new YouTubeMusicTrack(
                videoSearch.Url,
                videoSearch.Title,
                videoSearch.Author.ChannelTitle,
                videoSearch.Author.ChannelUrl,
                videoSearch.Thumbnails.FirstOrDefault()?.Url ?? null,
                videoSearch.Duration.GetValueOrDefault()
            );
        }

        public static async Task<IResult> SearchOnSpotify(string link)
        {
            if (_spotify == null)
                return new ErrorSearchResult("The Spotify client is not loaded.");

            if (!_spotify.IsValid)
                return new ErrorSearchResult("The Spotify client is not initialized.");

            return await _spotify.Search(link);
        }

        public static SearchType DetermineType(string query)
        {
            try
            {
                if (Uri.TryCreate(query, UriKind.Absolute, out Uri uri))
                {
                    if (uri.Host.Contains("spotify") || uri.Host.Contains("spoti.fy"))
                        return SearchType.SpotifyLink;

                    if (uri.Host.Contains("youtube") || uri.Host.Contains("youtu.be"))
                        return SearchType.YouTubeLink;
                }

                return SearchType.YouTubeQuery;
            }
            catch
            {
                return SearchType.YouTubeQuery;
            }
        }

        private static SearchFilter GetFilter(string query)
        {
            try
            {
                if (query.Contains("youtube.com/playlist?list=") || query.Contains("&list=") || PlaylistId.TryParse(query).HasValue)
                    return SearchFilter.Playlist;

                if (query.Contains("/watch?v=") || query.Contains("/v/") || query.Contains("youtu.be/") || VideoId.TryParse(query).HasValue)
                    return SearchFilter.Video;

                return SearchFilter.None;
            }
            catch
            {
                return SearchFilter.None;
            }
        }
    }
}