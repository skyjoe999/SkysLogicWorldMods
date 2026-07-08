using System.Collections.Generic;
using LogicAPI.Data.BuildingRequests;
using LogicWorld.BuildingManagement;

namespace SkysGeneralLib.Client.BuildRequests;

public static class BuildRequestExtension
{
    public static void Send(
        this BuildRequest request,
        BuildRequestManager.ReceiptReceivedCallback receiptReceivedCallback = null
    ) => BuildRequestManager.SendBuildRequest(request, receiptReceivedCallback);

    public static void SendNoUndo(
        this BuildRequest request,
        BuildRequestManager.ReceiptReceivedCallback receiptReceivedCallback = null,
        bool requestUndoActions = false
    ) => BuildRequestManager.SendBuildRequestWithoutAddingToUndoStack(
        request,
        receiptReceivedCallback,
        requestUndoActions
    );
    public static void SendAllRequests(
    this IReadOnlyList<BuildRequest> requests, // yea but I already had the other 2...
    BuildRequestManager.ReceiptReceivedCallback receiptReceivedCallback = null
    ) => BuildRequestManager.SendManyBuildRequestsAsMultiUndoItem(requests, receiptReceivedCallback);
}