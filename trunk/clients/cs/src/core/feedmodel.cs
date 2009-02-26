/* Copyright (c) 2006 Google Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/


using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Net; 
using Google.GData.Client;
using Google.GData.Extensions;
using System.Collections.Generic;
using Google.GData.Extensions.AppControl;


namespace Google.GData.Client
{
    //////////////////////////////////////////////////////////////////////
    /// <summary>a generic Feed class
    /// </summary> 
    //////////////////////////////////////////////////////////////////////
    public class Feed<T> where T: Entry, new()
    {
        AtomFeed af;
        bool paging;
        int  maximum = -1; 
        int  numberRetrieved=0; 
        Service service; 
        FeedQuery query; 


        /// <summary>
        /// default constructor that takes the underlying atomfeed
        /// </summary>
        /// <param name="af"></param>
        public Feed(AtomFeed af)
        {
            this.af = af; 
        }

        /// <summary>
        /// constructs a new feed object based on a service and a query
        /// </summary>
        /// <param name="service"></param>
        /// <param name="q"></param>
        public Feed(Service service, FeedQuery q)
        {
            this.service = service;
            this.query = q; 
        }

        /// <summary>
        /// returns the used feed object
        /// </summary>
        /// <returns></returns>
        public AtomFeed AtomFeed
        {
            get
            {
                if (this.af == null)
                {
                    if (this.service != null && this.query != null)
                    {
                        this.af = this.service.Query(query);
                    }
                }
                return this.af;
            }
        }

       /// <summary>
       /// if set to true will cause the feed to add more data when you iterate over it's entries
       /// </summary>
       /// <returns></returns>
       public bool AutoPaging
       {
           get 
           {
               return this.paging;
           }
           set
           {
               this.paging = value;
           }
       }


   
       /// <summary>
       /// returns the position in the real feed of the first entry in this feed
       /// </summary>
       /// <returns>an int indicating the start in the feed</returns>
       public int StartIndex
       {
           get
           {
               if (this.AtomFeed != null)
               {
                   return this.AtomFeed.StartIndex;
               }
               return -1;  
           }
       }

       /// <summary>
       /// returns the setup paging size of this feed. If you set AutoPaging to true
       /// this is the size that is used to get more results
       /// </summary>
       /// <returns></returns>
       public int PageSize
       {
           get
           {
               if (this.AtomFeed != null)
               {
                   return this.AtomFeed.ItemsPerPage;
               }
               return -1; 
           }
       }

       /// <summary>
       /// returns the number of entries the server believes the feed holds
       /// </summary>
       /// <returns></returns>
       public int TotalResults
       {
           get
           {
               if (this.AtomFeed != null)
               {
                   return this.AtomFeed.TotalResults;
               }
               return -1; 
           }
       }


       /// <summary>
       /// the maxium number of entries to be retrieved. This is normally 
       /// setup using the RequestSettings when the feed is constructed.
       /// </summary>
       /// <returns></returns>
       public int Maximum
       {
           get
           {
               return this.maximum;
           }
           set
           {
               this.maximum = value; 
           }
       }


       /**
       <summary>
        returns the initial list of entries.This page is the data
        you got from the Requestobject and will remain constant.
        Unless you set AutoPaging to true, in that case:
        This will go back to the server and fetch data again if
        needed. Example. If you pagesize is 30, you get an initial set of 
        30 entries. While enumerating, when reaching 30, the code will go 
        to the server and get the next 30 rows. It will continue to do so
        until the server reports no more rows available.
        Note that you should cache the entries returned in a list of your own
        if you want to access them more than once, as this one does no caching on
        it's own.
        </summary>
         <example>
                The following code illustrates a possible use of   
                the <c>Entries</c> property:  
                <code>    
                  YouTubeRequestSettings settings = new YouTubeRequestSettings("yourApp", "yourClient", "yourKey", "username", "pwd");
                  YouTubeRequest f = new YouTubeRequest(settings);
                  Feed&lt;Playlist&gt; feed = f.GetPlaylistsFeed(null);
                  foreach (Vidoe v in feed.Entries)
                </code>
            </example>
        <returns></returns> 
        */
        public IEnumerable<T> Entries
        {
            get
            {
                bool looping;

                // if we have iterated once before, we need to reset
                if (this.numberRetrieved > 0 && this.paging == false && this.service != null)
                {
                    this.af = null; 
                }
                if (this.AtomFeed == null)
                    yield break;

                this.numberRetrieved = 0; 

                do
                {
                    looping = af.NextChunk != null && this.paging == true;
                    foreach (AtomEntry e in af.Entries)
                    {
                        T t = new T();
                        if (t != null)
                        {
                            t.AtomEntry = e; 
                            this.numberRetrieved++; 
                            yield return t; 
                        }
                        if (this.Maximum > 0 && this.numberRetrieved >= this.Maximum)
                        {
                            yield break; 
                        }
                    }
                    if (looping)
                    {
                        FeedQuery q = new FeedQuery(this.AtomFeed.NextChunk);
                        this.af = this.AtomFeed.Service.Query(q);
                    }
                } while (looping);
            }
        }
    }
    //end of public class Feed


    /// <summary>
    /// the Entry class is the base class for all Feed of T type feeds
    /// it encapsulates the AtomEntry
    /// </summary>
    /// <returns></returns>
    public abstract class Entry
    {
        private AtomEntry e; 

        /// <summary>
        ///  default public constructor, needed for generics. You should not use that one, but use the
        /// CreateInstance method for the entry you want to create
        /// </summary>
        /// <returns></returns>
        public Entry()
        {
        }

        /// <summary>override for ToString, returns the Entries Title</summary> 
        public override string ToString()
        {
            return this.Title;
        }

        /// <summary>
        /// needs to be subclassed to ensure the creation of the corrent AtomEntry based
        /// object
        /// </summary>
        protected abstract void EnsureInnerObject();

        /// <summary>
        ///  the original AtomEntry object that this object is standing in for
        /// </summary>
        /// <returns></returns>
        public AtomEntry AtomEntry
        {
            get
            {
                return this.e;
            }
            set 
            {
                this.e = value; 
            }
        }

        /// <summary>
        /// returns the Id of an entry
        /// </summary>
        public string Id
        {
            get
            {
                EnsureInnerObject();
                return this.e.Id.AbsoluteUri;
            }
            set
            {
                EnsureInnerObject();
                this.e.Id = new AtomId(value);
            }
        }

        /// <summary>
        /// returns the value of the self uri as a string
        /// </summary>
        /// <returns></returns>
        public string Self
        {
            get
            {
                EnsureInnerObject();
                if (this.e.SelfUri != null)
                {
                    return this.e.SelfUri.ToString();
                }
                return null;
            }
        }


        /// <summary>
        /// the title of the Entry. 
        /// </summary>
        /// <returns></returns>
        public virtual string Title
        {
            get 
            {
                EnsureInnerObject();
                return this.e.Title.Text;
            }
            set 
            {
                EnsureInnerObject();
                this.e.Title.Text = value; 
            }
        }

        /// <summary>
        /// returns the appControl sublement
        /// </summary>
        public AppControl AppControl
        {
            get
            {
                EnsureInnerObject();
                return this.e.AppControl;
            }
            set
            {
                EnsureInnerObject();
                this.e.AppControl = value;
            }
        }


        /// <summary>
        /// returns the appControl sublement
        /// </summary>
        public bool IsDraft
        {
            get
            {
                EnsureInnerObject(); 
                return this.e.IsDraft; 
            }
        }


        /// <summary>
        ///  returns the first author name in the atom.entry.authors collection
        /// </summary>
        /// <returns></returns>
        public string Author
        {
            get
            {
                EnsureInnerObject();
                if (this.e.Authors.Count > 0 && this.e.Authors[0] != null)
                {
                    return this.e.Authors[0].Name;
                }
                return null;
            }
            set
            {
                EnsureInnerObject();
                AtomPerson p = null; 
                if (this.e.Authors.Count == 0)
                {
                    p = new AtomPerson(AtomPersonType.Author);
                    this.e.Authors.Add(p);
                }
                else
                {
                    p = this.e.Authors[0];
                }
                p.Name = value; 
            }
        }

        /// <summary>
        /// returns the string representation of the atom.content element
        /// </summary>
        /// <returns></returns>
        public string Content
        {
            get
            {
                EnsureInnerObject();
                return this.e.Content.Content;
            }
            set
            {
                EnsureInnerObject();
                this.e.Content.Content = value;
            }
        }

        /// <summary>
        /// returns the string representation of the atom.Summary element
        /// </summary>
        /// <returns></returns>
        public string Summary
        {
            get
            {
                EnsureInnerObject();
                return this.e.Summary.Text;
            }
            set
            {
                EnsureInnerObject();
                this.e.Summary.Text = value;
            }
        }

        /// <summary>
        /// just a thin layer on top of the existing updated of the 
        /// underlying atomentry
        /// </summary>
        public DateTime Updated
        {
            get
            {
                EnsureInnerObject();
                return this.e.Updated;
            }
            set
            {
                EnsureInnerObject();
                this.e.Updated = value;
            }
        }


        /// <summary>
        /// this returns the batch data for the inner atom object
        /// </summary>
        /// <returns></returns>
        public GDataBatchEntryData BatchData
        {
            get
            {
                EnsureInnerObject();
                return this.e.BatchData;
            }
            set
            {
                EnsureInnerObject();
                this.e.BatchData = value;
            }
        }

    }

   
    /// <summary>
    /// Base requestsettings class. Takes credentials, applicationsname
    /// and supports pagesizes and autopaging. This class is used to initialize a 
    /// <seealso cref="FeedRequest&lt;T&gt;"/> object.
    /// </summary>
    /// <returns></returns>
    public class RequestSettings
    {
        private string applicationName;
        private GDataCredentials credentials; 
        private string authSubToken; 
        private int pageSize = -1;
        private int max = -1; 
        private bool autoPage;
        private int timeout = -1; 

        /// <summary>
        /// an unauthenticated use case
        /// </summary>
        /// <param name="applicationName"></param>
        /// <returns></returns>
        public RequestSettings(string applicationName)
        {
            this.applicationName = applicationName;
        }

        /// <summary>
        ///  a constructor for client login use cases
        /// </summary>
        /// <param name="applicationName">The name of the application</param>
        /// <param name="userName">the user name</param>
        /// <param name="passWord">the password</param>
        /// <returns></returns>
        public RequestSettings(string applicationName, string userName, string passWord)
        {
            this.applicationName = applicationName;
            this.credentials = new GDataCredentials(userName, passWord);
        }

        /// <summary>
        ///  a constructor for client login use cases
        /// </summary>
        /// <param name="applicationName">The name of the application</param>
        /// <param name="credentials">the user credentials</param>
        /// <returns></returns>
        public RequestSettings(string applicationName, GDataCredentials credentials)
        {
            this.applicationName = applicationName;
            this.credentials = credentials;
        }


        /// <summary>
        /// a constructor for a web application authentication scenario
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="authSubToken"></param>
        /// <returns></returns>
        public RequestSettings(string applicationName, string authSubToken)
        {
            this.applicationName = applicationName;
            this.authSubToken = authSubToken; 
        }


        /// <summary>
        /// returns the Credentials in case of a client login scenario
        /// </summary>
        /// <returns></returns>
        public GDataCredentials Credentials
        {
            get
            {
                return this.credentials;
            }
        }

        /// <summary>
        /// returns the authsub token to use for a webapplication scenario
        /// </summary>
        /// <returns></returns>
        public string AuthSubToken
        {
            get
            {
                return this.authSubToken;
            }
        }

        /// <summary>
        /// returns the application name
        /// </summary>
        /// <returns></returns>
        public string Application
        {
            get
            {
                return this.applicationName;
            }
        }

        /// <summary>
        /// the pagesize specifies how many entries should be retrieved per call. If not set, 
        /// the server default will be used. Set it either to -1 (for default) or any value &gt; 0
        /// to set the pagesize to something the server should honor. Note, that this set's the 
        /// max-results parameter on the query, and the server is free to ignore that and give you less 
        /// entries than you have requested. 
        /// </summary>
        ///  <example>
        ///         The following code illustrates a possible use of   
        ///          the <c>PageSize</c> property:  
        ///          <code>    
        ///           YouTubeRequestSettings settings = new YouTubeRequestSettings("yourApp", "yourClient", "yourKey", "username", "pwd");
        ///            settings.PageSize = 50; 
        ///  </code>
        ///  </example>
        /// <returns></returns>
        public int PageSize
        {
            get
            {
                return this.pageSize;
            }
            set
            {
                this.pageSize = value;
            }
        }

        /// <summary>
        /// AutoPaging specifies if a feed iterator should return to the server to fetch more data 
        /// automatically. If set to false, a loop over feed.Entries will stop when the currently 
        /// fetched set of data reaches it's end.  This is false by default. <seealso cref="RequestSettings.Maximum"/>
        /// 
        /// </summary>
        ///  <example>
        ///         The following code illustrates a possible use of   
        ///          the <c>AutoPaging</c> property:  
        ///          <code>    
        ///           YouTubeRequestSettings settings = new YouTubeRequestSettings("yourApp", "yourClient", "yourKey", "username", "pwd");
        ///            settings.AutoPaging = true; 
        ///  </code>
        ///  </example>
        /// <returns></returns>
        public bool AutoPaging
        {
            get
            {
                return this.autoPage;
            }
            set
            {
                this.autoPage = value; 
            }
        }

        /// <summary>
        /// the Maximum specifies how many entries should be retrieved in total. This works together with 
        /// <seealso cref="RequestSettings.AutoPaging"/>. If set, AutoPaging of a feed will stop when the 
        /// specified amount of entries was iterated over. If Maximum is smaller than  PageSize (<seealso cref="RequestSettings.PageSize"/>), 
        ///  an exception is thrown. The default is -1 (ignored). 
        /// </summary>
        ///  <example>
        ///         The following code illustrates a possible use of   
        ///          the <c>Maximum</c> property:  
        ///          <code>    
        ///           YouTubeRequestSettings settings = new YouTubeRequestSettings("yourApp", "yourClient", "yourKey", "username", "pwd");
        ///            settings.PageSize = 50; 
        ///            settings.AutoPaging = true;
        ///            settings.Maximum = 2000; 
        ///  </code>
        ///  </example>
        /// <returns></returns>
        public int Maximum
        {
            get
            {
                return this.max;
            }
            set
            {
                if (value < this.PageSize)
                {
                    throw new ArgumentException("Maximum must be greater or equal to PageSize"); 
                }
                this.max = value;
            }
        }


        /// <summary>get's and set's the Timeout property used for the created
        /// HTTPRequestObject in milliseconds. if you set it to -1 it will stick 
        /// with the default of the HTPPRequestObject. From MSDN:
        /// The number of milliseconds to wait before the request times out. 
        /// The default is 100,000 milliseconds (100 seconds).</summary>   
        ///  <example>
        ///         The following code illustrates a possible use of   
        ///          the <c>Timeout</c> property:  
        ///          <code>    
        ///           YouTubeRequestSettings settings = new YouTubeRequestSettings("yourApp", "yourClient", "yourKey", "username", "pwd");
        ///            settings.Timout = 10000000;
        ///  </code>
        ///  </example>
        /// <returns></returns>
        public int Timeout
        {
            get
            {
                return this.timeout;
            }
            set
            {
                this.timeout = value;
            }
        }
    }


    /// <summary>
    /// the enum used for Get of T requests
    /// </summary>
    public enum FeedRequestType
    {
        /// <summary>
        /// returns the next feed chunk if there is more data
        /// </summary>
        Next,
        /// <summary>
        /// returns the previous feed chunk if there is data before
        /// </summary>
        Prev,
        /// <summary>
        /// refreshes the actual feed chunk by going to the server and retrieving it again
        /// </summary>
        Refresh
    }


    /// <summary>
    /// base class for Request objects.
    /// </summary>
    /// <returns></returns>
    public abstract class FeedRequest<T> where T : Service
    {
        private RequestSettings settings;
        private T atomService; 


       

        /// <summary>
        /// default constructor based on a RequestSettings object
        /// </summary>
        /// <param name="settings"></param>
        public FeedRequest(RequestSettings settings)
        {
            this.settings = settings; 
        }

        /// <summary>
        /// prepares the created service based on the settings 
        /// </summary>
        protected void PrepareService()
        {
            PrepareService(this.atomService);
        }

        /// <summary>
        /// prepares the passed in service by setting the authentication credentials and the timeout settings
        /// </summary>
        /// <param name="s"></param>
        protected void PrepareService(Service s)
        {
            if (settings.Credentials != null)
            {
                s.Credentials = settings.Credentials;
            }
#if WindowsCE || PocketPC
#else
            if (settings.AuthSubToken != null)
            {
                GAuthSubRequestFactory authFactory = new GAuthSubRequestFactory(s.ServiceIdentifier, settings.Application);
                authFactory.UserAgent = authFactory.UserAgent + "--IEnumerable";
                authFactory.Token = settings.AuthSubToken; 
                s.RequestFactory = authFactory;
            }
            else 
            {
                GDataGAuthRequestFactory authFactory = s.RequestFactory as GDataGAuthRequestFactory;
                if (authFactory != null)
                {
                    authFactory.UserAgent = authFactory.UserAgent + "--IEnumerable";
                }
            }

            if (settings.Timeout != -1)
            {
                GDataRequestFactory f  = s.RequestFactory as GDataRequestFactory;
                if (f != null)
                {
                    f.Timeout = settings.Timeout;
                }
            }
#endif
        }

        /// <summary>
        /// creates a query object and set's it up based on the settings object.
        /// </summary>
        /// <typeparam name="Y"></typeparam>
        /// <param name="uri"></param>
        /// <returns></returns>
        protected Y PrepareQuery<Y>(string uri) where Y: FeedQuery, new()
        {
            Y query = new Y(); 
            query.BaseAddress = uri; 

            PrepareQuery(query);
            return query; 
        }

        /// <summary>
        /// prepares the passed in query objects properties based on the settings
        /// </summary>
        /// <param name="q"></param>
        protected void PrepareQuery(FeedQuery q)
        {
            if (this.settings.PageSize != -1)
            {
                q.NumberToRetrieve = this.settings.PageSize; 
            }
        }

        /// <summary>
        /// creates a feed of Y object based on the query and the settings
        /// </summary>
        /// <typeparam name="Y"></typeparam>
        /// <param name="q"></param>
        /// <returns></returns>
        protected virtual Feed<Y> PrepareFeed<Y>(FeedQuery q) where Y : Entry, new()
        {
             // AtomFeed feed = this.atomService.Query(q);
             // Feed<Y> f = new Feed<Y>(feed);
             Feed<Y> f = new Feed<Y>(this.atomService, q);
             f.AutoPaging = this.settings.AutoPaging;
             f.Maximum   = this.settings.Maximum;
             return f;
        }

        /// <summary>
        /// gets a feed object of type T
        /// </summary>
        /// <typeparam name="Y"></typeparam>
        /// <param name="q"></param>
        /// <returns></returns>
        public Feed<Y> Get<Y>(FeedQuery q) where Y: Entry, new()
        {
            return PrepareFeed<Y>(q);  
        }

        /// <summary>
        /// gets a feed object of type T
        /// </summary>
        /// <typeparam name="Y"></typeparam>
        /// <param name="uri">The Uri to retrieve</param>
        /// <returns></returns>
        public Feed<Y> Get<Y>(Uri uri) where Y : Entry, new()
        {
            FeedQuery q = new FeedQuery(uri.AbsoluteUri);
            return PrepareFeed<Y>(q);
        }


        /// <summary>
        /// returns a new feed based on the operation passed in.  This is useful if you either do not use
        /// autopaging, or want to move to previous parts of the feed, or get a refresh of the current feed
        /// </summary>
        ///  <example>
        ///         The following code illustrates a possible use of   
        ///          the <c>Get</c> method:  
        ///          <code>    
        ///           YouTubeRequestSettings settings = new YouTubeRequestSettings("yourApp", "yourClient", "yourKey", "username", "pwd");
        ///            YouTubeRequest f = new YouTubeRequest(settings);
        ///             Feed&lt;Playlist&gt; feed = f.GetPlaylistsFeed(null);
        ///             Feed&lt;Playlist&gt; next = f.Get&lt;Playlist&gt;(feed, FeedRequestType.Next);
        ///  </code>
        ///  </example>
        /// <param name="feed">the original feed</param>
        /// <param name="operation">an requesttype to indicate what to retrieve</param>
        /// <returns></returns>
        public Feed<Y> Get<Y>(Feed<Y> feed, FeedRequestType operation) where Y: Entry, new()
        {
            Feed<Y> f = null; 
            string spec = null; 

            if (feed == null)
            {
                throw new ArgumentNullException("feed was null");
            }

            if (feed.AtomFeed == null)
            {
                throw new ArgumentNullException("feed.AtomFeed was null");
            }

            switch (operation)
            {
                case FeedRequestType.Next:
                    spec = feed.AtomFeed.NextChunk; 
                    break;
                case FeedRequestType.Prev:
                    spec = feed.AtomFeed.PrevChunk;
                    break;
                case FeedRequestType.Refresh:
                    spec = feed.AtomFeed.Self; 
                    break;
            }
            if (String.IsNullOrEmpty(spec) == false)
            {
                FeedQuery q =  new FeedQuery(spec);
                if (operation == FeedRequestType.Refresh)
                {
                    ISupportsEtag ise = feed.AtomFeed as ISupportsEtag;
                    if (ise != null && ise.Etag != null)
                    {
                        q.Etag = ise.Etag;
                    }
                }
                f = PrepareFeed<Y>(q); 
            }

            return f; 
        }


        /// <summary>
        /// performs a batch operation. 
        /// </summary>
        /// <param name="feed">the original feed, used to find the batch endpoing </param>
        /// <param name="entries">List of entries of type Y, that are to be batched</param>
        /// <returns></returns>
        public Feed<Y> Batch<Y>(List<Y> entries, Feed<Y> feed) where Y : Entry, new()
        {
            if (feed == null ||
                feed.AtomFeed == null)
            {
                throw new ArgumentNullException("Invalid feed passed in");
            }

            if (feed.AtomFeed.Batch == null)
            {
                throw new ArgumentException("Feed has no valid batch endpoint");
            }
            return this.Batch(entries, new Uri(feed.AtomFeed.Batch));

        }


        /// <summary>
        /// performs a batch operation. 
        /// </summary>
        /// <param name="batchUri">the batch endpoint of the service</param>
        /// <param name="entries">List of entries of type Y, that are to be batched</param>
        /// <returns></returns>
        public Feed<Y> Batch<Y>(List<Y> entries, Uri batchUri) where Y: Entry, new()
        {
            if (entries.Count > 0)
            {
                AtomFeed batchFeed = new AtomFeed(batchUri, null);
                foreach (Y e in entries)
                {
                    batchFeed.Entries.Add(e.AtomEntry);
                }
                AtomFeed resultFeed = this.Service.Batch(batchFeed, batchUri);
                Feed<Y> f = new Feed<Y>(resultFeed);
                return f;
            }
            return null;
        }



        /// <summary>
        /// returns the service instance that is used
        /// </summary>
        public T Service
        {
            get
            {
                return this.atomService;
            }
            set
            {
                this.atomService = value;
            }
        }


        /// <summary>
        /// returns a refreshed version of the entry you passed in, by going back to the server and
        /// requesting this resource again
        /// </summary>
        ///  <example>
        ///         The following code illustrates a possible use of   
        ///          the <c>Get</c> method:  
        ///          <code>    
        ///           YouTubeRequestSettings settings = new YouTubeRequestSettings("yourApp", "yourClient", "yourKey", "username", "pwd");
        ///            YouTubeRequest f = new YouTubeRequest(settings);
        ///             Feed&lt;Playlist&gt; feed = f.GetPlaylistsFeed(null);
        ///             Feed&lt;Playlist&gt; next = f.Get&lt;Playlist&gt;(feed, FeedRequestType.Next);
        ///  </code>
        ///  </example>
        /// <param name="entry">the entry to get again</param>
        /// <returns></returns>
        public Y Get<Y>(Y entry) where Y: Entry, new()
        {

            Feed<Y> f = null;
            Y r = null; 

            if (entry == null)
            {
                throw new ArgumentNullException("entry was null");
            }

            if (entry.AtomEntry == null)
            {
                throw new ArgumentNullException("entry.AtomEntry was null");
            }

            string spec =entry.AtomEntry.SelfUri.ToString();

            if (String.IsNullOrEmpty(spec) == false)
            {
                FeedQuery q = new FeedQuery(spec);
                ISupportsEtag ise = entry.AtomEntry as ISupportsEtag;
                if (ise != null && ise.Etag != null)
                {
                    q.Etag = ise.Etag;
                }
                f = PrepareFeed<Y>(q); 
            }
            // this should be a feed of one... 

            foreach (Y y in f.Entries)
            {
                r =y; 
            }
            return r; 
        }



        /// <summary>
        ///  sends the data back to the server. 
        /// </summary>
        /// <returns>the reflected entry from the server if any given</returns>
        public Y Update<Y>(Y entry) where Y: Entry, new()
        {
            if (entry == null)
                throw new ArgumentNullException("Entry was null");

            if (entry.AtomEntry == null)
                throw new ArgumentNullException("Entry.AtomEntry was null");

            Y r = null;
            AtomEntry ae = this.Service.Update(entry.AtomEntry);
           
            if (ae != null)
            {
                r = new Y();
                r.AtomEntry = ae;
            }
            return r; 
        }

        /// <summary>
        ///  deletes the Entry from the Server
        /// </summary>
        public void Delete<Y>(Y entry) where Y : Entry, new()
        {
            if (entry == null)
                throw new ArgumentNullException("Entry was null");

            if (entry.AtomEntry == null)
                throw new ArgumentNullException("Entry.AtomEntry was null");

            entry.AtomEntry.Delete();
        }

        /// <summary>
        /// takes the given Entry and inserts its into the server
        /// </summary>
        /// <returns>the reflected entry from the server if any given</returns>
        public Y Insert<Y>(Uri address, Y entry) where Y : Entry, new()
        {
            if (entry == null)
                throw new ArgumentNullException("Entry was null");

            if (entry.AtomEntry == null)
                throw new ArgumentNullException("Entry.AtomEntry was null");

            if (address == null)
                throw new ArgumentNullException("Entry was null");
          
            Y r = null;
            AtomEntry ae = this.Service.Insert(address, entry.AtomEntry);
            if (ae != null)
            {
                r = new Y();
                r.AtomEntry = ae;
            }
            return r;
        }

        

        /// <summary>
        /// takes the given Entry and inserts its into the server
        /// </summary>
        /// <returns>the reflected entry from the server if any given</returns>
        public Y Insert<Y>(Feed<Y> feed, Y entry) where Y : Entry, new()
        {
            if (entry == null)
                throw new ArgumentNullException("Entry was null");

            if (entry.AtomEntry == null)
                throw new ArgumentNullException("Entry.AtomEntry was null");

            if (feed == null)
                throw new ArgumentNullException("Feed was null");

            Y r = null;
            AtomEntry ae = this.Service.Insert(feed.AtomFeed, entry.AtomEntry);
            if (ae != null)
            {
                r = new Y();
                r.AtomEntry = ae;
            }
            return r;
        }

        /// <summary>
        /// the Settings property returns the RequestSettings object that was used to construct this FeedRequest. 
        /// It can be used to alter properties like AutoPaging etc, inbetween Feed creations. 
        /// </summary>
        ///  <example>
        ///         The following code illustrates a possible use of   
        ///          the <c>Settings</c> property:  
        ///          <code>   
        ///         YouTubeRequestSettings settings = new YouTubeRequestSettings("NETUnittests", this.ytClient, this.ytDevKey, this.ytUser, this.ytPwd);
        ///         YouTubeRequest f = new YouTubeRequest(settings);
        ///         Feed&lt;Video&gt; feed = f.GetStandardFeed(YouTubeQuery.MostPopular);
        ///         foreach (Video v in feed.Entries)
        ///         {
        ///             f.Settings.PageSize = 50;
        ///             f.Settings.AutoPaging = true;
        ///             Feed&lt;Comment&gt; list = f.GetComments(v);
        ///             foreach (Comment c in list.Entries)
        ///              {
        ///                 Assert.IsTrue(v.AtomEntry != null);
        ///                  Assert.IsTrue(v.Title != null);
        ///             }
        ///           }
        ///  </code>
        ///  </example>
        /// <returns></returns>
        public  RequestSettings Settings
        {
            get
            {
                return this.settings;
            }
        }
    }
}
