using System.Collections.Generic;
using JimmysUnityUtilities;
using LogicAPI.Server.Components;
using UnityEngine;

namespace SkysSockets.Server;

public class VirtualSocketHolder(LogicComponent Root)
{
    public static readonly (Vector3, Vector3, Vector3, Vector3) DefaultBlueSquarePoints = (
        new Vector3(-0.1f, 0f, -0.1f), new Vector3(-0.1f, 0f, 0.1f), new Vector3(0.1f, 0f, 0.1f), new Vector3(0.1f, 0f, -0.1f)
    );
    public static readonly (Vector3, Vector3, Vector3, Vector3) ChubbyBlueSquarePoints = (
        new Vector3(-0.4f, 0f, -0.4f), new Vector3(-0.4f, 0f, 0.4f), new Vector3(0.4f, 0f, 0.4f), new Vector3(0.4f, 0f, -0.4f)
    );
    

    public List<VirtualSocket> Sockets = [];
    private (Vector3, Vector3, Vector3, Vector3) BlueSquarePoints = DefaultBlueSquarePoints;

    public void OnComponentMoved()
    {
        foreach (var socket in Sockets)
            socket.OnComponentMoved();
    }
    public void OnComponentDestroyed()
    {
        foreach (var socket in Sockets)
            socket.OnComponentDestroyed();
        Sockets = [];
    }

    public void GenerateSockets(
        IReadOnlyList<IInputPeg> inputPegs,
        IReadOnlyList<Vector3> relativePositions,
        IReadOnlyList<Quaternion> relativeRotations)
    {
        Clear();
        for (var i = 0; i < inputPegs.Count; i++)
            Sockets.Add(new VirtualSocket(
                inputPegs[i],
                Root,
                relativePositions[i],
                relativeRotations[i],
                BlueSquarePoints
            ));
        OnComponentMoved();
    }

    public void SetBlueSquarePoints((Vector3, Vector3, Vector3, Vector3) blueSquarePoints)
    {
        BlueSquarePoints = blueSquarePoints;
        foreach (var socket in Sockets)
            socket.SetBlueSquarePoints(BlueSquarePoints);
    }

    public void Clear()
    {
        foreach (var socket in Sockets)
            socket.OnComponentDestroyed();
        Sockets = [];
    }

    public bool HasSockets() => !Sockets.IsEmpty();
}