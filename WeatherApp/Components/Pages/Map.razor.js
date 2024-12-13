export function map_init(lat = 55.6761, lon = 12.5683, zoom = 10) {
    var map = L.map('map').
        setView([lat, lon], zoom);

    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    var marker; 

    map.on('click', function (e) {
        if (marker) {
            map.removeLayer(marker);
        }

        marker = L.marker(e.latlng).addTo(map);
    });

    const response = await fetch('/api/WindData');
    const jsonData = await response.json();

    var velocityLayer = L.velocityLayer({
        displayValues: true,
        displayOptions: {
            velocityType: 'Global Wind',
            position: 'bottomleft',
            emptyString: 'No wind data'
        },
        data: jsonData, // Pass the data array directly
        maxVelocity: 20,   // adjust as needed
        velocityScale: 0.01, // tweak to control vector size
    });

    velocityLayer.addTo(map);
}

