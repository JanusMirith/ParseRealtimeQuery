using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Parse;
using Parse.Infrastructure;
using System.IO;
using Parse.LiveQuery;
using Parse.Abstractions.Internal;
using System.Linq;

/// <summary>
/// This is an example class that shows how to use the realtime query
/// </summary>
public class ParseExample : MonoBehaviour
{
    /// <summary>
    /// A reference to the Parse Client
    /// </summary>
    ParseClient parseClient;

    /// <summary>
    /// A reference to the parse Live Client
    /// </summary>
    ParseLiveQueryClient parseLiveClient;

    /// <summary>
    /// Our example realtime Query
    /// </summary>
    private RealtimeQuery<ParseObject> realtimeQuery;

    /// <summary>
    /// Our example realtime Query
    /// </summary>
    private Dictionary<string, GameObject> cubeDict;

    /// <summary>
    /// This is just a little bool that tracks whether we are subcribed, it is a bit of a hack to show the emuilation of entry for new subcribers to the realtimequyery
    /// </summary>
    public bool IsSubscribed;

    // Start is called before the first frame update
    void Start()
    {
        // Make the normal client
        parseClient = new ParseClient(
            new ServerConnectionData
            {
                ApplicationID = "FILL ME IN!!!",
                ServerURI = "HTTP FILL ME IN .COM",
                Key = "ADD THIS TOO", // This is unnecessary if a value for MasterKey is specified.
                Headers = new Dictionary<string, string>
                {
                }
            },
            new LateInitializedMutableServiceHub { },
            new MetadataMutator
            {
                EnvironmentData = new EnvironmentData { OSVersion = SystemInfo.operatingSystem, Platform = $"Unity {Application.unityVersion} on {SystemInfo.operatingSystemFamily}", TimeZone = System.TimeZoneInfo.Local.StandardName },
                HostManifestData = new HostManifestData { Name = Application.productName, Identifier = Application.productName, ShortVersion = Application.version, Version = Application.version }
            }

            , new AbsoluteCacheLocationMutator
            {

                CustomAbsoluteCacheFilePath = $"{Application.persistentDataPath.Replace('/', Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}Parse.cache"
            }
        );

        // Setup the instance
        parseClient.Publicize();

        // Make the live client
        parseLiveClient = new ParseLiveQueryClient(new System.Uri($"wss://SOME LIVE SERVER")); //This is the URI to your live server

        // Lets listen to some events for the lifecycle of the Live Query
        parseLiveClient.OnConnected += ParseLiveClient_OnConnected;
        parseLiveClient.OnDisconnected += ParseLiveClient_OnDisconnected;
        parseLiveClient.OnLiveQueryError += ParseLiveClient_OnLiveQueryError;
        parseLiveClient.OnSocketException += ParseLiveClient_OnSocketException;


        // Setup the instance
        parseLiveClient.Publicize();


        // Lets do a quick test to see how it all works
        AuthUser();
    }

    /// <summary>
    /// You must disconnect when unity closes
    /// </summary>
    private void OnDestroy()
    {
        realtimeQuery.Destroy();
        parseLiveClient.Disconnect();
    }


    private void ParseLiveClient_OnSocketException(System.Exception obj)
    {
        Debug.LogError("ParseLiveClient_OnSocketException");
        Debug.LogException(obj);
    }

    private void ParseLiveClient_OnLiveQueryError(System.Exception obj)
    {
        Debug.LogError("ParseLiveClient_OnLiveQueryError");
        Debug.LogException(obj);
    }

    private void ParseLiveClient_OnDisconnected()
    {
        Debug.Log("ParseLiveClient_OnDisconnected");
    }

    private void ParseLiveClient_OnConnected()
    {
        Debug.Log("ParseLiveClient_OnConnected");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            // Lets unsubscribe from the realtime Query
            if (IsSubscribed)
            {
                // get rid of those cubes
                foreach (GameObject go in cubeDict.Values)
                {
                    GameObject.Destroy(go);
                }
                cubeDict = new Dictionary<string, GameObject>();

                realtimeQuery.OnCreate -= CubQ_OnCreate;
                realtimeQuery.OnDelete -= CubQ_OnDelete;
                realtimeQuery.OnEnter -= CubQ_OnEnter;
                realtimeQuery.OnLeave -= CubQ_OnLeave;
                realtimeQuery.OnUpdate -= CubQ_OnUpdate;
                IsSubscribed = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            if (!IsSubscribed)
            {
                // Lets subscribe to the realtime Query while it is already running
                realtimeQuery.OnCreate += CubQ_OnCreate;
                realtimeQuery.OnDelete += CubQ_OnDelete;
                realtimeQuery.OnEnter += CubQ_OnEnter;
                realtimeQuery.OnLeave += CubQ_OnLeave;
                realtimeQuery.OnUpdate += CubQ_OnUpdate;
                IsSubscribed = true;

                // If the query is auready running, all valid objects are returned on the onEnter event
            }
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Make a new object and push it to the database
            ParseObject testObject = new ParseObject("Cubes");
            testObject.Set("XPos", Random.Range(-10f, 10f));
            testObject.Set("YPos", Random.Range(-10f, 10f));
            testObject.Set("ZPos", Random.Range(-10f, 10f));
            testObject.SaveAsync();
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            // Lets modify one of the objects we know about
            ParseObject testObject = realtimeQuery.WatchedObjects.Values.ElementAt(Random.Range(0, realtimeQuery.WatchedObjects.Count)); // Look this line is a tad clunky, but 
            testObject.Set("XPos", Random.Range(-10f, 10f));
            testObject.Set("YPos", Random.Range(-10f, 10f));
            testObject.Set("ZPos", Random.Range(-10f, 10f));

            // Send it back to the database
            testObject.SaveAsync();
        }
    }

    /// <summary>
    /// This creates and Auths a User
    /// </summary>
    public async void AuthUser()
    {
        // Create a user, save it, and authenticate with it.
        //await parseClient.SignUpAsync(username: "Test", password: "Test");  //You only need to do this once

        // Authenticate the user.
        ParseUser user = await parseClient.LogInAsync(username: "test", password: "test");
        //Debug.Log(parseClient.GetCurrentUser().SessionToken);
        
        // Lets start that Query
        StartRealtimeQuery();
    }

    /// <summary>
    /// Creates a realtime query and listens to the events
    /// </summary>
    private void StartRealtimeQuery()
    {
        // First make a query
        ParseQuery<ParseObject> query = parseClient.GetQuery("Cubes");

        // query = query.WhereGreaterThan("YPos", 2);

        //Make a dict to store the cubes
        cubeDict = new Dictionary<string, GameObject>();

        // Create a realtime query
        realtimeQuery = new RealtimeQuery<ParseObject>(query, slowAndSafe: true);
        realtimeQuery.OnCreate += CubQ_OnCreate;
        realtimeQuery.OnDelete += CubQ_OnDelete;
        realtimeQuery.OnEnter += CubQ_OnEnter;
        realtimeQuery.OnLeave += CubQ_OnLeave;
        realtimeQuery.OnUpdate += CubQ_OnUpdate;
        IsSubscribed = true; // lets remember this so we can play around with the event emulation
    }

    /// <summary>
    /// Somthing about one of our objects has been changed
    /// </summary>
    /// <param name="obj">the changed object</param>
    private void CubQ_OnUpdate(ParseObject obj)
    {
        UpdateCube(obj);
    }

    /// <summary>
    /// We have had a object leave our query
    /// </summary>
    /// <param name="obj">the object that is leaveing</param>
    private void CubQ_OnLeave(ParseObject obj)
    {
        DeleteCube(obj);
    }

    /// <summary>
    /// We have had a new object enter our query
    /// </summary>
    /// <param name="obj">the object that entered the query</param>
    private void CubQ_OnEnter(ParseObject obj)
    {
        CreateCube(obj);
    }

    /// <summary>
    /// One of the objects we were looking at was deleted
    /// </summary>
    /// <param name="obj">the object that was deleted</param>
    private void CubQ_OnDelete(ParseObject obj)
    {
        DeleteCube(obj);
    }

    /// <summary>
    /// A new object has been created that matches the query
    /// </summary>
    /// <param name="obj">the object that was created</param>
    private void CubQ_OnCreate(ParseObject obj)
    {
        CreateCube(obj);
    }

    /// <summary>
    /// Creates a new cube and updates it's values as provided by the database
    /// </summary>
    /// <param name="cubeParse">the parse object retreived</param>
    private void CreateCube(ParseObject cubeParse)
    {
        GameObject cc = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeDict[cubeParse.ObjectId] = cc;

        // update the values in the cube
        UpdateCube(cubeParse);
    }

    /// <summary>
    /// Delete the cube
    /// </summary>
    /// <param name="cubeParse">the cube we want to delete</param>
    private void DeleteCube(ParseObject cubeParse)
    {
        GameObject.Destroy(cubeDict[cubeParse.ObjectId]);
        cubeDict.Remove(cubeParse.ObjectId);
    }

    /// <summary>
    /// Updates the cube based on the values provided
    /// </summary>
    /// <param name="cubeParse">the updated data</param>
    private void UpdateCube(ParseObject cubeParse)
    {
        // build a new vector3 
        Vector3 pos = new Vector3(cubeParse.Get<float>("XPos"), cubeParse.Get<float>("YPos"), cubeParse.Get<float>("ZPos"));
        // get our cube
        GameObject cube = cubeDict[cubeParse.ObjectId];
        // set that pos
        cube.transform.position = pos;
    }
}
