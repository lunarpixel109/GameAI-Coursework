using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class MapCamera : MonoBehaviour
{
	private const float ZoomAmount = 2.5f;
	private const float MinGridCellsAtMinZoom = 15f;

	private float minZoom = ZoomAmount;

	private float cameraDefaultOrthographicSize;

	private Vector3 startTranformPosition;
	private Vector3 startScreenPosition;
	private Vector3 startWorldPosition;
	private bool previousMouseButton = false;

	private void Start()
	{
		var camera = Camera.main;
		var cameraPosition = camera.transform.position;
		cameraPosition.x = MapRenderer.MapWidthWorldSize * 0.5f;
		cameraPosition.y = MapRenderer.MapHeightWorldSize * 0.5f;
		camera.transform.position = cameraPosition;

		cameraDefaultOrthographicSize = MapRenderer.MapHeightWorldSize * 0.5f;
		camera.orthographicSize = cameraDefaultOrthographicSize;
		minZoom = ((MinGridCellsAtMinZoom / Map.MapHeight) * MapRenderer.MapHeightWorldSize) * 0.5f;
	}

	private void Update()
	{
		var camera = Camera.main;
		var currentMouseButton = Input.GetMouseButton(0);

		if (previousMouseButton == false && currentMouseButton)
		{
			// On first click store start position to be used in later frames for positioning
			startScreenPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.0f);
			startWorldPosition = camera.ScreenToWorldPoint(startScreenPosition);
			startTranformPosition = transform.position;
		}
		else if (currentMouseButton)
		{
			// Reset Camera to starting position and then calculate the difference in movement from the start
			transform.position = startTranformPosition;
			var newScreenPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.0f);
			var newWorldPosition = camera.ScreenToWorldPoint(newScreenPosition);
			var worldPositionDelta = startWorldPosition - newWorldPosition;

			// Apply difference from start
			transform.position = startTranformPosition + worldPositionDelta;
		}

		previousMouseButton = currentMouseButton;

		// Zooming
		if (Input.mouseScrollDelta.y > 0.0f && camera.orthographicSize > minZoom)
		{
			camera.orthographicSize -= ZoomAmount;
		}
		else if (Input.mouseScrollDelta.y < 0.0f && camera.orthographicSize <= cameraDefaultOrthographicSize - ZoomAmount)
		{
			camera.orthographicSize += ZoomAmount;
		}
	}
}
