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
}

