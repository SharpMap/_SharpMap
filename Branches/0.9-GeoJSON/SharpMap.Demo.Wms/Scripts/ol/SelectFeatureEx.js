OpenLayers.Control.SelectFeatureEx = OpenLayers.Class(OpenLayers.Control.SelectFeature, {

    initialize: function(layers, options) {
        OpenLayers.Control.SelectFeature.prototype.initialize.apply(this, [layers, options]);
        this.events.addEventType("clickFeature");
    },

    clickFeature: function(feature) {
        OpenLayers.Control.SelectFeature.prototype.clickFeature.apply(this, [feature]);
        this.events.triggerEvent("clickFeature", { feature: feature });
    },

    CLASS_NAME: "OpenLayers.Control.SelectFeatureEx"
});