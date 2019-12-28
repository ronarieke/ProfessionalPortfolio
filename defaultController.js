var root = "Backend";
angular.module('SimbaForceApp').controller('defaultCtrl', function ($scope, $http) {
    $scope.data = defaultModel;
    $scope.ParseCountries = function(){
        $http({
            url:root+"/CountryStatistics",
            method:"GET"
        }).then(function(response){
            var dat = response.data;
            $scope.data.countries = dat;
        })
    }
    $scope.CollectInstrumentPerformance = function(){
        $http({
            url:root+"/InstrumentStatistics",
            method:"GET"
        }).then(function(response){
            var dat = response.data;
            $scope.data.instruments = dat;
            $scope.data.countries = $scope.ParseCountries()
        })
    }
    $scope.LoginFeature = function () {
        $http({
            url: root + "/LoginFeature",
            method: "POST",
            data: $scope.data.User
        }).then(function (response) {
            console.log(response);
            $scope.data.User = response.data;
            $scope.CollectInstrumentPerformance();
        })
    }
});