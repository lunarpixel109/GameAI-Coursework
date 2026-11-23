using System;
using System.Collections.Generic;
using UnityEngine;

public static class AStarPathfinder
{
	private sealed class Node
	{
		public int Index;
		public int CostFromStart;
		public int EstimatedCost;
		public int Cost => CostFromStart + EstimatedCost;
	}

	public static List<Vector3> FindPath(Map map, Vector3 startPosition, Vector3 targetPosition)
	{
		if (map == null)
		{
			throw new ArgumentNullException(nameof(map));
		}

		int startX = Mathf.Clamp(Mathf.RoundToInt(startPosition.x), 0, Map.MapWidth - 1);
		int startY = Mathf.Clamp(Mathf.RoundToInt(startPosition.y), 0, Map.MapHeight - 1);
		int targetX = Mathf.Clamp(Mathf.RoundToInt(targetPosition.x), 0, Map.MapWidth - 1);
		int targetY = Mathf.Clamp(Mathf.RoundToInt(targetPosition.y), 0, Map.MapHeight - 1);

		return FindPath(map, startX, startY, targetX, targetY);
	}

	public static List<Vector3> FindPath(Map map, int startX, int startY, int targetX, int targetY)
	{
		if (map == null)
		{
			throw new ArgumentNullException(nameof(map));
		}

		if (!IsInside(startX, startY) || !IsInside(targetX, targetY))
		{
			return null;
		}

		int startIndex = map.MapIndex(startX, startY);
		int targetIndex = map.MapIndex(targetX, targetY);
		if (!map.IsNavigatable(startIndex) || !map.IsNavigatable(targetIndex))
		{
			return null;
		}

		int[] costFromStart = new int[Map.MapSize];
		Array.Fill(costFromStart, int.MaxValue);
		int[] cameFrom = new int[Map.MapSize];
		Array.Fill(cameFrom, -1);
		bool[] closed = new bool[Map.MapSize];
		costFromStart[startIndex] = 0;

		List<Node> open = new List<Node>();
		open.Add(new Node
		{
			Index = startIndex,
			CostFromStart = 0,
			EstimatedCost = Heuristic(startX, startY, targetX, targetY)
		});

		while (open.Count > 0)
		{
			int bestOpenIndex = 0;
			for (int i = 1; i < open.Count; ++i)
			{
				if (open[i].Cost < open[bestOpenIndex].Cost ||
					(open[i].Cost == open[bestOpenIndex].Cost &&
					 open[i].EstimatedCost < open[bestOpenIndex].EstimatedCost))
				{
					bestOpenIndex = i;
				}
			}

			Node current = open[bestOpenIndex];
			open.RemoveAt(bestOpenIndex);

			if (current.CostFromStart != costFromStart[current.Index])
			{
				continue;
			}
			if (closed[current.Index])
			{
				continue;
			}
			if (current.Index == targetIndex)
			{
				return ReconstructWorldPath(map, cameFrom, startIndex, targetIndex);
			}

			closed[current.Index] = true;
			int currentX = map.MapIndexToX(current.Index);
			int currentY = map.MapIndexToY(current.Index);

			TryAddNeighbour(map, open, costFromStart, cameFrom, closed, current, currentX - 1, currentY, targetX, targetY);
			TryAddNeighbour(map, open, costFromStart, cameFrom, closed, current, currentX + 1, currentY, targetX, targetY);
			TryAddNeighbour(map, open, costFromStart, cameFrom, closed, current, currentX, currentY - 1, targetX, targetY);
			TryAddNeighbour(map, open, costFromStart, cameFrom, closed, current, currentX, currentY + 1, targetX, targetY);
		}

		return null;
	}

	private static void TryAddNeighbour(Map map, List<Node> open, int[] costFromStart, int[] cameFrom,
		bool[] closed, Node current, int neighbourX, int neighbourY, int targetX, int targetY)
	{
		if (!IsInside(neighbourX, neighbourY))
		{
			return;
		}

		int neighbourIndex = map.MapIndex(neighbourX, neighbourY);
		if (!map.IsNavigatable(neighbourIndex) || closed[neighbourIndex])
		{
			return;
		}

		int movementCost = GetTerrainMoveCost(map.GetTerrainAt(neighbourIndex));
		int tentativeCost = costFromStart[current.Index] + movementCost;
		if (tentativeCost >= costFromStart[neighbourIndex])
		{
			return;
		}

		costFromStart[neighbourIndex] = tentativeCost;
		cameFrom[neighbourIndex] = current.Index;
		open.Add(new Node
		{
			Index = neighbourIndex,
			CostFromStart = tentativeCost,
			EstimatedCost = Heuristic(neighbourX, neighbourY, targetX, targetY)
		});
	}

	private static List<Vector3> ReconstructWorldPath(Map map, int[] cameFrom, int startIndex, int targetIndex)
	{
		List<int> indices = new List<int>();
		int currentIndex = targetIndex;

		while (currentIndex != -1)
		{
			indices.Add(currentIndex);
			if (currentIndex == startIndex)
			{
				break;
			}
			currentIndex = cameFrom[currentIndex];
		}

		if (indices.Count == 0 || indices[indices.Count - 1] != startIndex)
		{
			return null;
		}

		indices.Reverse();
		List<Vector3> path = new List<Vector3>(indices.Count);
		foreach (int index in indices)
		{
			path.Add(new Vector3(map.MapIndexToX(index), map.MapIndexToY(index), 0.0f));
		}
		return path;
	}

	private static int GetTerrainMoveCost(Map.Terrain terrain)
	{
		switch (terrain)
		{
			case Map.Terrain.Mud:
				return 2;
			case Map.Terrain.Water:
				return 4;
			default:
				return 1;
		}
	}

	private static int Heuristic(int x, int y, int targetX, int targetY)
	{
		return Mathf.Abs(x - targetX) + Mathf.Abs(y - targetY);
	}

	private static bool IsInside(int x, int y)
	{
		return x >= 0 && x < Map.MapWidth && y >= 0 && y < Map.MapHeight;
	}
}
