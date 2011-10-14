$(document).ready(function() {
    var po = org.polymaps, mercator, container, map, layer;

    mercator = new GlobalMercator();
    container = $('#map').get(0).appendChild(po.svg('svg'));
    map = po.map()
        .container(container)
        .center({ lat: 40.7723, lon: -73.9529 })
        .zoom(10)
        .add(po.interact())
        .add(po.hash());
    map.container().setAttribute("class", "PuBu");       
    
    map.add(po.image().url(
        po.url(['http://{S}tile.cloudmade.com', '/1a1b06b230af4efdbb989ea99e9841af', '/998/256/{Z}/{X}/{Y}.png'].join(''))
        .hosts(['a.', 'b.', 'c.', ''])));
    
    layer = po.geoJson().url(function(data) {
        var bounds, url;
        bounds = mercator.TileLatLonBounds(data.column, data.row, data.zoom)
        url = ['/wms.ashx?MAP_TYPE=PM&HEIGHT=256&WIDTH=256&STYLES=&',
            'CRS=EPSG%3A4326&FORMAT=text%2Fjson&SERVICE=WMS&VERSION=1.3.0&REQUEST=GetMap&',
            'EXCEPTIONS=application%2Fvnd.ogc.se_inimage&transparent=true&',
            'LAYERS=poly_landmarks', '&BBOX=', bounds[1], ',', -bounds[2], ',', bounds[3], ',', -bounds[0]
        ].join('');
        return url;
    }).on("load", function(e) {
        var node, parent;
        $.each(e.features, function() {
            node = document.createTextNode(this.data.properties.LANAME);
            this.element.setAttribute("class", "q3-4");
            parent = po.svg("title").appendChild(node).parentNode;
            this.element.appendChild(parent);
        });
    })
    layer.container().setAttribute("class", "poly_landmarks");
    map.add(layer);

    map.add(po.grid());
    map.add(po.compass().pan('short'));
});






















