export async function map_init(lat = 55.6761, lon = 12.5683, zoom = 10) {
    const isTouchDevice = (
        ('ontouchstart' in window) ||
        (navigator.maxTouchPoints > 0) ||
        (navigator.msMaxTouchPoints > 0)
    );

    var map = L.map("map").setView([lat, lon], zoom);

    // Example fetch
    const response = await fetch('/api/WindData');
    const jsonData = await response.json();

    L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
    }).addTo(map);

    let marker;

    map.on("click", function (e) {
        if (isTouchDevice) {
            if (marker) {
                map.removeLayer(marker);
            }
            marker = L.marker(e.latlng).addTo(map);
        }
    });

    var velocityLayer = L.velocityLayer({
        displayValues: true,
        displayOptions: {
            velocityType: 'Global Wind',
            position: 'bottomleft',
            emptyString: 'No wind data'
        },
        data: jsonData, // pass the data array directly
        maxVelocity: 20,   // control max veclocity
        velocityScale: 0.05, // tweak to control vector size
    });

    velocityLayer.addTo(map);
}

