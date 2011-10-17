$(document).ready(function() {
    var lon = -73.9529;
    var lat = 40.7723;
    var zoom = 10;

    var map, cloudmade, center, url;

    map = new L.Map('map');
    cloudmade = new L.TileLayer('http://{s}.tile.cloudmade.com/1a1b06b230af4efdbb989ea99e9841af/997/256/{z}/{x}/{y}.png', { maxZoom: 18 });

    center = new L.LatLng(lat, lon);
    map.setView(center, zoom).addLayer(cloudmade);

    url = ['/wms.ashx?MAP_TYPE=PM&HEIGHT=256&WIDTH=256&STYLES=&',
            'CRS=EPSG%3A4326&FORMAT=text%2Fjson&SERVICE=WMS&VERSION=1.3.0&REQUEST=GetMap&',
            'EXCEPTIONS=application%2Fvnd.ogc.se_inimage&transparent=true&',
            'LAYERS=poly_landmarks,tiger_roads,poi',
            '&BBOX=-74,40,-70,42'
        ].join('')
    $.getJSON(url,
        function(e) {
            var layer, type;
            layer = new L.GeoJSON();
            layer.on('featureparse', function(e) {
                if (!e.layer.setStyle)
                    return;

                type = e.geometryType;
                if (type === 'Polygon' || type === 'MultiPolygon') {
                    e.layer.setStyle({
                        color: 'rgb(0,0,180)',
                        weight: 4,
                        opacity: 0.6
                    });
                }
                else if (type === 'LineString' || type === 'MultiLineString') {
                    e.layer.setStyle({
                        color: 'rgb(180,0,0)',
                        weight: 1,
                        opacity: 0.9
                    });
                }
                else if (type === 'Point' || type === 'MultiPoint') {
                }
            });
            $.each(e.features, function() {
                layer.addGeoJSON(this);
            })
            map.addLayer(layer);
        });
});






















