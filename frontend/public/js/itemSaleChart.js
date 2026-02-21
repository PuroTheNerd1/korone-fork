'use strict';
// This code is not transpiled or obfuscated. It must remain comptaible with most browsers.
// @ts-ignore
window.RobloxItemChartLibrary = (function LoadItemSaleCharts() {
  function addScript(scriptPath, callback) {
    var el = document.createElement('script');
    el.setAttribute('src', scriptPath);
    el.async = false;
    el.defer = false;
    el.onload = callback;
    document.body.appendChild(el);
  }
  return {
    loadChart: function (rapChart, volumeChart) {
      // @ts-ignore
      if (window.$) {
        loadCharts();
      } else {
        // Add Jquery
        addScript('/js/jquery-3.6.0.min.js', function () {
          // Add flot
          addScript('/js/flot-0.8.3/jquery.flot.js', function () {
            // Add flot time
            addScript('/js/flot-0.8.3/jquery.flot.time.js', function () {
              loadCharts();
            });
          });
        });
      }

      function loadCharts() {
        var exists = document.getElementById("placeholder");
        if (exists !== null && exists !== undefined) {
          var children = exists.children;
          for (var i = 0; i < children.length; i++) {
            children[i].remove();
          }
        }
        var d1 = rapChart;
        var d2 = volumeChart;

        function numberWithCommas(x) {
          var parts = x.toString().split(".");
          parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ",");
          return parts.join(".");
        }

        function formatGraphTicks(v, axis) {
          var result;
          if (v > 1000000000) {
            result = (v / 1000000000).toFixed(axis.tickDecimals) + "B R$";
          } else if (v > 1000000) {
            result = (v / 1000000).toFixed(axis.tickDecimals) + "M R$";
          } else {
            result = v.toFixed(axis.tickDecimals);
          }
          return numberWithCommas(result) + " R$";
        }

        function plotCharts(days) {
          var minTime = new Date().getTime() - (86400 * days * 1000);
          $.plot($("#placeholder"), [
            { data: d1, label: "Avg Sales Price (R$)", color: "#008000", lines: { lineWidth: 3 } }
          ], {
            xaxis: { mode: 'time', timeformat: "%m/%d", min: minTime },
            legend: { position: 'nw' },
            yaxis: { labelWidth: 40, tickFormatter: formatGraphTicks }
          });
          $.plot($("#volumegraph"), [
            { data: d2, label: "Volume", yaxis: 1, color: "#A4A4C8", bars: { show: true } }
          ], {
            xaxis: { mode: 'time', ticks: [], min: minTime },
            legend: { position: 'nw' },
            yaxis: { labelWidth: 40, minTickSize: 1, tickDecimals: 0, ticks: [] }
          });
        }

        // Default to 180 days
        plotCharts(180);

        // Dropdown select handler
        $("#daysSelect").off("change").on("change", function () {
          plotCharts(parseInt($(this).val()));
        });
      }
    },
  }
})();
