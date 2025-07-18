﻿// Copyright Valkey GLIDE Project Contributors - SPDX Identifier: Apache-2.0

using Valkey.Glide.Internals;

using static Valkey.Glide.ConnectionConfiguration;
using static Valkey.Glide.Internals.FFI;

namespace Valkey.Glide;

/// <summary>
/// Basic class. Please use one of the following implementations:
/// <list type="bullet">
/// <item><see cref="RandomRoute"/></item>
/// <item><see cref="AllNodesRoute"/></item>
/// <item><see cref="AllPrimariesRoute"/></item>
/// <item><see cref="SlotIdRoute"/></item>
/// <item><see cref="SlotKeyRoute"/></item>
/// <item><see cref="ByAddressRoute"/></item>
/// </list>
/// </summary>
public abstract class Route
{
    public abstract class SingleNodeRoute : Route { }

    public abstract class MultiNodeRoute : Route { }

    /// <summary>
    /// Route request to a random node.<br />
    /// <b>Warning:</b> Don't use it with write commands, they could be routed to a replica (RO) node and fail.
    /// </summary>
    public sealed class RandomRoute : SingleNodeRoute
    {
        internal override FFI.Route ToFfi() => new(RouteType.Random);
    }

    /// <summary>
    /// Route request to all nodes.<br />
    /// <b>Warning:</b> Don't use it with write commands, they could be routed to a replica (RO) node and fail.
    /// </summary>
    public sealed class AllNodesRoute : MultiNodeRoute
    {
        internal override FFI.Route ToFfi() => new(RouteType.AllNodes);
    }

    /// <summary>
    /// Route request to all primary nodes.
    /// </summary>
    public sealed class AllPrimariesRoute : MultiNodeRoute
    {
        internal override FFI.Route ToFfi() => new(RouteType.AllPrimaries);
    }

    /// <inheritdoc cref="RandomRoute"/>
    public static readonly RandomRoute Random = new();
    /// <inheritdoc cref="AllNodesRoute"/>
    public static readonly AllNodesRoute AllNodes = new();
    /// <inheritdoc cref="AllPrimariesRoute"/>
    public static readonly AllPrimariesRoute AllPrimaries = new();

    /// <summary>
    /// Defines type of the node being addressed.
    /// </summary>
    public enum SlotType : uint
    {
        /// <summary>
        /// Address a primary node.
        /// </summary>
        Primary,
        /// <summary>
        /// Address a replica node.
        /// </summary>
        Replica,
    }

    /// <summary>
    /// Request routing configuration overrides the <see cref="ReadFromStrategy"/> connection configuration.<br />
    /// If <see cref="SlotType.Replica"/> is used, the request will be routed to a replica, even if the strategy is <see cref="ReadFromStrategy.Primary"/>.
    /// </summary>
    /// <param name="slotId">Slot number. There are 16384 slots in a Valkey cluster, and each shard manages a slot range.
    /// Unless the slot is known, it's better to route using <see cref="SlotKeyRoute"/>.</param>
    /// <param name="slotType">Defines type of the node being addressed.</param>
    public class SlotIdRoute(int slotId, SlotType slotType) : SingleNodeRoute
    {
        public readonly int SlotId = slotId;
        public new readonly SlotType SlotType = slotType;

        internal override FFI.Route ToFfi() => new(RouteType.SlotId, slotIdInfo: (SlotId, SlotType));
    }

    /// <summary>
    /// Request routing configuration overrides the <see cref="ReadFromStrategy"/> connection configuration.<br />
    /// If <see cref="SlotType.Replica"/> is used, the request will be routed to a replica, even if the strategy is <see cref="ReadFromStrategy.Primary"/>.
    /// </summary>
    /// <param name="slotKey">The request will be sent to nodes managing this key.</param>
    /// <param name="slotType">Defines type of the node being addressed.</param>
    public class SlotKeyRoute(string slotKey, SlotType slotType) : SingleNodeRoute
    {
        public readonly string SlotKey = slotKey;
        public new readonly SlotType SlotType = slotType;

        internal override FFI.Route ToFfi() => new(RouteType.SlotKey, slotKeyInfo: (SlotKey, SlotType));
    }

    /// <summary>
    /// Routes a request to a node by its address.
    /// </summary>
    public class ByAddressRoute : SingleNodeRoute
    {
        public readonly string Host;
        public readonly int Port;

        /// <summary>
        /// Create a route using hostname/address and port.<br />
        /// <paramref name="host"/> is the preferred endpoint as shown in the output of the <c>CLUSTER SLOTS</c> command.
        /// </summary>
        /// <param name="host">A hostname or IP address.</param>
        /// <param name="port">A port.</param>
        public ByAddressRoute(string host, int port)
        {
            Host = host;
            Port = port;
        }

        /// <summary>
        /// Create a route using address string formatted as <c>"address:port"</c>.
        /// <paramref name="host"/> is the preferred endpoint as shown in the output of the <c>CLUSTER SLOTS</c> command.
        /// </summary>
        /// <param name="host">Address in format <c>"address:port"</c>.</param>
        /// <exception cref="ArgumentException"></exception>
        public ByAddressRoute(string host)
        {
            string[] parts = host.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException("No port provided, and host is not in the expected format 'hostname:port'. Received: " + host);
            }
            Host = parts[0];
            Port = int.Parse(parts[1]);
        }

        internal override FFI.Route ToFfi() => new(RouteType.ByAddress, address: (Host, Port));
    }

    internal Route() { }

    internal abstract FFI.Route ToFfi();
}
