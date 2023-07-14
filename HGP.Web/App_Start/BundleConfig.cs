#region Using

using System.Web.Optimization;

#endregion

namespace HGP.Web
{
    public static class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle("~/bundles/adminstyles").Include(
                "~/content/css/bootstrap.min.css",
                "~/content/css/demo.min.css",
                "~/content/css/font-awesome.min.css",
                "~/content/css/smartadmin-production-plugins.min.css",
                "~/content/css/smartadmin-production.min.css",
                "~/content/css/smartadmin-skins.min.css",
                "~/content/css/your_style.css"
                ));

            bundles.Add(new ScriptBundle("~/bundles/smartadmin").Include(
                "~/scripts/app.config.js",
                "~/scripts/plugin/jquery-touch/jquery.ui.touch-punch.min.js",
                "~/scripts/bootstrap/bootstrap.min.js",
                "~/scripts/notification/SmartNotification.min.js",
                "~/scripts/smartwidgets/jarvis.widget.min.js",
                "~/scripts/plugin/jquery-validate/jquery.validate.min.js",
                "~/scripts/plugin/jquery-validate/jquery.validate.unobtrusive.min.js",
                "~/scripts/plugin/masked-input/jquery.maskedinput.min.js",
                "~/scripts/plugin/select2/select2.min.js",
                "~/scripts/plugin/bootstrap-slider/bootstrap-slider.min.js",
                "~/scripts/plugin/bootstrap-progressbar/bootstrap-progressbar.min.js",
                "~/scripts/plugin/msie-fix/jquery.mb.browser.min.js",
                "~/scripts/plugin/fastclick/fastclick.min.js",
                "~/scripts/plugin/x-editable/moment.min.js",
                "~/scripts/app.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                "~/scripts/plugin/jquery-validate/jquery.validate.min.js",
                "~/scripts/plugin/jquery-validate/jquery.validate.unobtrusive.min.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/timezone").Include(
            "~/scripts/plugin/moment/moment.min.js",
            "~/scripts/plugin/moment/moment-timezone-with-data.min.js",
            "~/Scripts/jstz.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/full-calendar").Include(
                "~/scripts/plugin/fullcalendar/jquery.fullcalendar.min.js"
                ));
         
            bundles.Add(new ScriptBundle("~/bundles/charts").Include(
                "~/scripts/plugin/easy-pie-chart/jquery.easy-pie-chart.min.js",
                "~/scripts/plugin/sparkline/jquery.sparkline.min.js",
                "~/scripts/plugin/morris/morris.min.js",
                "~/scripts/plugin/morris/raphael.min.js",
                "~/scripts/plugin/flot/jquery.flot.cust.min.js",
                "~/scripts/plugin/flot/jquery.flot.resize.min.js",
                "~/scripts/plugin/flot/jquery.flot.time.min.js",
                "~/scripts/plugin/flot/jquery.flot.fillbetween.min.js",
                "~/scripts/plugin/flot/jquery.flot.orderBar.min.js",
                "~/scripts/plugin/flot/jquery.flot.pie.min.js",
                "~/scripts/plugin/flot/jquery.flot.tooltip.min.js",
                "~/scripts/plugin/dygraphs/dygraph-combined.min.js",
                "~/scripts/plugin/chartjs/chart.min.js",
                "~/scripts/waypoints/jquery.waypoints.min.js"
                ));

            bundles.Add(new ScriptBundle("~/bundles/ko").Include(
                "~/Scripts/knockout-{version}.js",
                "~/Scripts/knockout.mapping-latest.js",
                "~/Scripts/knockout-switch-case.js",
                "~/Scripts/date.format.js",
                "~/Scripts/knockout/DateExtender.js",
                "~/scripts/plugin/datatables/jquery.dataTables.min.js",
                "~/scripts/plugin/datatables/dataTables.tableTools.min.js",
                "~/scripts/waypoints/jquery.waypoints.min.js",
                "~/scripts/waypoints/infinite.min.js",
                "~/scripts/waypoints/sticky.min.js",
                "~/scripts/numeral.js"
                ));

            bundles.Add(new ScriptBundle("~/bundles/datatables").Include(
                "~/scripts/plugin/datatables/jquery.dataTables.min.js",
                "~/scripts/plugin/datatables/dataTables.colVis.min.js",
                "~/scripts/plugin/datatables/dataTables.tableTools.min.js",
                "~/scripts/plugin/datatables/dataTables.bootstrap.min.js",
                "~/scripts/plugin/datatable-responsive/datatables.responsive.min.js"
                ));

            bundles.Add(new ScriptBundle("~/bundles/jq-grid2020").Include(
                "~/scripts/jqgrid2020/plugins/ui.multiselect.js",
                "~/scripts/jqgrid2020/js/i18n/grid.locale-en.js",
                "~/scripts/jqgrid2020/js/jquery.jqGrid.min.js"
                ));

            bundles.Add(new ScriptBundle("~/bundles/forms").Include(
                "~/scripts/plugin/jquery-form/jquery-form.min.js",
                "~/Scripts/jquery.validate.*",
                "~/Scripts/jquery.validate.unobtrusive.*"
                ));

            bundles.Add(new ScriptBundle("~/bundles/smart-chat").Include(
                "~/scripts/smart-chat-ui/smart.chat.ui.min.js",
                "~/scripts/smart-chat-ui/smart.chat.manager.min.js"
                ));

            bundles.Add(new ScriptBundle("~/bundles/vector-map").Include(
                "~/scripts/plugin/vectormap/jquery-jvectormap-1.2.2.min.js",
                "~/scripts/plugin/vectormap/jquery-jvectormap-world-mill-en.js"
                ));

            bundles.Add(new ScriptBundle("~/bundles/summernote").Include(
                "~/scripts/summernote/summernote.js"
                ));
            bundles.Add(new StyleBundle("~/bundles/summernotestyles").Include(
                "~/content/css/summernote.css"
                ));
            bundles.Add(new ScriptBundle("~/bundles/dropzone").Include(
                "~/Scripts/dropzone-5.5.0/dist/min/dropzone.min.js"
            ));
            bundles.Add(new StyleBundle("~/bundles/dropzonestyles").Include(
                "~/Scripts/dropzone-5.5.0/dist/dropzone.css"
            ));

            BundleTable.EnableOptimizations = false;
        }
    }
}