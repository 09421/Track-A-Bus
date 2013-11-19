<%@ Page Language="C#" %>

<!DOCTYPE html>



<html>
<head title="Route Tool">
    <meta name="viewport" content="initial-scale=1.0, user-scalable=no" />
    <style type="text/css">
        html {
            height: 100%;
        }

        body {
            height: 100%;
            margin: 0;
            padding: 0;
        }

        #map-canvas {
            height: 100%;
        }
    </style>
    <script type="text/javascript" src="http://ajax.googleapis.com/ajax/libs/jquery/1.2.6/jquery.min.js"></script>
    <script src="https://maps.googleapis.com/maps/api/js?v=3.exp&sensor=false"></script>

    <script type="text/javascript">

        var map;
        var directionsDisplay;
        var directionsService;
        var stepDisplay;

        function initialize() {
            // Instantiate a directions service.

            var featureOpts = [{
                featureType: 'poi',
                stylers: [
                    { visibility: 'off' }]
            }
            ];

            //Creates the DirectionsService
            directionsService = new google.maps.DirectionsService();

            // Create a map and center it on Aarhus.
            var Aarhus = new google.maps.LatLng(56.155955, 10.205011);
            var mapOptions = {
                zoom: 13,
                mapTypeId: google.maps.MapTypeId.ROADMAP,
                center: Aarhus,
                streetViewControl: false,
                styles: featureOpts
            };
            map = new google.maps.Map(document.getElementById('map-canvas'), mapOptions);

            // Create a renderer for directions and bind it to the map.
            var rendererOptions = {
                map: map,
                draggable: true
            };
            directionsDisplay = new google.maps.DirectionsRenderer(rendererOptions);

            //Sets the mapto use the TransitLayer
            var transitLayer = new google.maps.TransitLayer();
            transitLayer.setMap(map);
            Debug.writeln("Map initialized");

            GetAllStops();
        }

        function GetAllStops() {
            $.ajax({
                type: "GET",
                url: "Route/GetStops",
                dataType: "json",
                success: function (result) {
                    var select = document.combo_box.FromLB;
                    select.options.length = 0;
                    for (var i = 0; i < result.length; i++) {
                        select.options.add(new Option(result[i].name, result[i].ID));
                    }
                }
            });
        }

        function move(tbFrom, tbTo) {
            arrFrom = new Array();
            arrTo = new Array();
            arrLU = new Array();
            var i;
            for (i = 0; i < tbTo.options.length; i++) {
                arrLU[tbTo.options[i].text] = tbTo.options[i].value;
                arrTo[i] = tbTo.options[i].text;
            }
            var fLength = 0;
            var tLength = arrTo.length;
            for (i = 0; i < tbFrom.options.length; i++) {
                arrLU[tbFrom.options[i].text] = tbFrom.options[i].value;
                if (tbFrom.options[i].selected && tbFrom.options[i].value != "") {
                    arrTo[tLength] = tbFrom.options[i].text;
                    tLength++;
                }
                else {
                    arrFrom[fLength] = tbFrom.options[i].text;
                    fLength++;
                }
            }

            tbFrom.length = 0;
            tbTo.length = 0;
            var ii;

            for (ii = 0; ii < arrFrom.length; ii++) {
                var no = new Option();
                no.value = arrLU[arrFrom[ii]];
                no.text = arrFrom[ii];
                tbFrom[ii] = no;
            }

            for (ii = 0; ii < arrTo.length; ii++) {
                var no = new Option();
                no.value = arrLU[arrTo[ii]];
                no.text = arrTo[ii];
                tbTo[ii] = no;
            }
        }

        function CreateRoute() {

            $.ajax({
                type: "POST",
                url: "Route/GetRouteLatLng",
                dataType: "json",
                success: function (result) {

                    if (MarkerArray.length != 0)
                        MarkerArray = [];

                    for (var i = 0; i < result.length; i++) {
                        var marker = new google.maps.Marker({
                            position: new google.maps.LatLng(result[i].Lat, result[i].Lng),
                            map: map,
                            title: result[i].ID.toString()
                        });
                    }
                }
            });
        }

        var MarkerArray = new Array();
        var arrFrom = new Array();
        var arrTo = new Array();
        var arrLU = new Array();

        google.maps.event.addDomListener(window, 'load', initialize);
    </script>
</head>
<body>
    <div style="float: right; width: 30%; height: 100%; text-align: left; padding-top: 10px;">
        <form name="combo_box">
            <table>
                <tr>
                    <td>
                        <select multiple size="15" name="FromLB" style="width: 150">
                        </select>
                    </td>
                    <td align="center" valign="middle">
                        <input type="button" onclick="move(this.form.FromLB, this.form.ToLB)"
                            value="->"><br />
                        <input type="button" onclick="move(this.form.ToLB, this.form.FromLB)"
                            value="<-">
                    </td>
                    <td>
                        <select multiple size="15" name="ToLB" style="width: 150">
                        </select>
                    </td>
                </tr>
            </table>
            <input id="clickMe" type="button" value="Create Route" onclick="CreateRoute();" style="float: left;" />
        </form>
    </div>
    <div id="map-canvas" style="float: left; width: 70%; height: 100%;"></div>
</body>
</html>
