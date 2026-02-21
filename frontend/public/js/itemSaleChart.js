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
        addScript('/js/jquery-3.6.0.min.js', function () {
          addScript('/js/flot-0.8.3/jquery.flot.js', function () {
            addScript('/js/flot-0.8.3/jquery.flot.time.js', function () {
              loadCharts();
            });
          });
        });
      }

      function loadCharts() {
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
            result = (v / 1000000000).toFixed(axis.tickDecimals) + "B";
          } else if (v > 1000000) {
            result = (v / 1000000).toFixed(axis.tickDecimals) + "M";
          } else if (v > 1000) {
            result = (v / 1000).toFixed(axis.tickDecimals) + "K";
          } else {
            result = v.toFixed(axis.tickDecimals);
          }
          return numberWithCommas(result);
        }

        // Tooltip element
        var $tooltip = $('#flot-rap-tooltip');
        if ($tooltip.length === 0) {
          $tooltip = $('<div id="flot-rap-tooltip"></div>').css({
            position: 'absolute',
            background: '#333',
            color: '#fff',
            padding: '2px 8px',
            borderRadius: '3px',
            fontSize: '12px',
            pointerEvents: 'none',
            display: 'none',
            zIndex: 9999,
          }).appendTo('body');
        }

        function plotCharts(days) {
          var minTime = new Date().getTime() - (86400 * days * 1000);

          // Scale volume to fill only the bottom ~20% of the chart height
          var priceMax = 0, volumeMax = 1;
          for (var i = 0; i < d1.length; i++) {
            if (d1[i][1] > priceMax) priceMax = d1[i][1];
          }
          for (var i = 0; i < d2.length; i++) {
            if (d2[i][1] > volumeMax) volumeMax = d2[i][1];
          }
          var scale = priceMax > 0 ? (priceMax * 0.18) / volumeMax : 1;
          var scaledVolume = [];
          for (var i = 0; i < d2.length; i++) {
            scaledVolume.push([d2[i][0], d2[i][1] * scale]);
          }

          $.plot($("#placeholder"), [
            {
              data: d1,
              color: "#008000",
              lines: { lineWidth: 2, fill: false },
              points: { show: false },
              shadowSize: 0
            },
            {
              data: scaledVolume,
              color: "#A4A4C8",
              bars: { show: true, lineWidth: 0, barWidth: 86400 * 0.7 * 1000 },
              shadowSize: 0
            }
          ], {
            xaxis: { mode: 'time', timeformat: "%m/%d", min: minTime },
            yaxis: { labelWidth: 45, tickFormatter: formatGraphTicks, min: 0 },
            legend: { show: false },
            grid: {
              borderWidth: 1,
              borderColor: '#ddd',
              hoverable: true,
              autoHighlight: true,
            }
          });
        }

        // Hover tooltip for the price line (series index 0)
        $("#placeholder").off("plothover").on("plothover", function (event, pos, item) {
          if (item && item.seriesIndex === 0) {
            var val = numberWithCommas(Math.round(item.datapoint[1]));
            $tooltip.html(val).css({ left: item.pageX + 8, top: item.pageY - 28 }).show();
          } else {
            $tooltip.hide();
          }
        });
        $("#placeholder").off("mouseleave").on("mouseleave", function () {
          $tooltip.hide();
        });

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
