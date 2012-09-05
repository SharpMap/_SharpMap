// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

// Based on ngpsql's ConnPoolDesign demo: http://pgfoundry.org/projects/npgsql
// The following is only proof-of-concept. A final implementation should be based
// on "src\Npgsql\NpgsqlConnectorPool.cs"

using System;
using System.Collections.Generic;
using System.Text;
using DelftTools.Utils.IO;

namespace SharpMap.Data.Providers.Pooling
{
	/// <summary>
	/// The ConnectorPool class implements the functionality for 
	/// the administration of the connectors. Controls pooling and
	/// sharing of connectors.
	/// </summary>
    [Obsolete("Move it tom some more generic leven (DelftTools.Utils) which will pool IFileBased entities")]
    internal class ConnectorPool
	{
		/// <summary>Unique static instance of the connector pool manager.</summary>
		internal static ConnectorPool ConnectorPoolManager = new ConnectorPool();
		/// <summary>List of unused, pooled connectors avaliable to the next RequestConnector() call.</summary>
		internal List<Connector> PooledConnectors;
		/// <summary>List of shared, in use connectors.</summary>
		internal List<Connector> SharedConnectors;
		/// <summary>
		/// Default constructor, creates a new connector pool object.
		/// Should only be used once in an application, since more 
		/// than one connector pool does not make much sense..
		/// </summary>
		internal ConnectorPool()
		{
			this.PooledConnectors = new List<Connector>();
			this.SharedConnectors = new List<Connector>();
		}

		/// <summary>
		/// Searches the shared and pooled connector lists for a
		/// matching connector object or creates a new one.
		/// </summary>
		/// <param name="entity">Entity to be pooled.</param>
		/// <param name="Shared">Allows multiple connections on a single connector. </param>
		/// <returns>A pooled connector object.</returns>
		internal Connector RequestConnector(IFileBased entity, bool Shared)
		{
			// if a shared connector is requested then the Shared
			// Connector List is searched first:
		    if (Shared)
		    {
		        foreach (Connector connector in this.SharedConnectors)
		        {
		            if (entity.Path == connector.PooledEntity.Path)
		            {
		                // Bingo!
		                // Return the shared connector to caller.
		                // The connector is already in use.
		                connector._ShareCount++;
		                return connector;
		            }
		        }
		    }

			// if a shared connector could not be found or a
			// nonshared connector is requested, then the pooled
			// (unused) connectors are beeing searched.
			foreach (Connector connector in this.PooledConnectors)
			{
				if (connector.PooledEntity.Path == entity.Path)
				{	// Bingo!
					// Remove the Connector from the pooled connectors list.
					this.PooledConnectors.Remove(connector);
					// Make the connector shared if requested					
					if (connector.Shared = Shared)
					{
						this.SharedConnectors.Add(connector);
						connector._ShareCount = 1;
					}
					// done...
					connector.InUse = true;
					return connector;
				}
			}

			// No suitable connector found, so create new one
			Connector NewConnector = new Connector(entity, Shared);

			// Shared connections must be added to the shared 
			// connectors list
			if (Shared)
			{
				this.SharedConnectors.Add(NewConnector);
				NewConnector._ShareCount = 1;
			}

			// and then returned to the caller
			NewConnector.InUse = true;
			NewConnector.Open();
			return NewConnector;
		}
	}
}