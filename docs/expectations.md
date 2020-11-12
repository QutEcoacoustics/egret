

Currently you can:

- check coordinates based on bounding box, centroid, or temporal span
- check event duration, bandwidth, or label
- check meta features like event count, check for no events, or check for no extra events


 ## Test types


 There are two test types. 
 
 ## Event tests
 
 Event tests look for events. You can specify different
 ways to find an event by using:
 - a bounding box (temporal and spectral bounds): `[start, low, end, high]`
 - a point/centroid (temporal and spectral midpoint): `[start, low]`
 - temporal range only: `[start, ~, end]`

 ## Segment tests