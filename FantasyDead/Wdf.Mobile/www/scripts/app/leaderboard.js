(function () {
    var app = angular.module('wdf.leaderboard', ['ngRoute']);

    app.controller('leaderboardController', ['$scope', '$rootScope', '$location', '$http', function ($scope, $rootScope, $location, $http) {
        document.addEventListener("deviceready", function () {
            $http.defaults.headers.common.Authorization = 'Bearer ' + $rootScope.user.Token;


            $scope.lbPage = 1;
            $scope.leaderboard = [];
            $scope.showing = 'all';
            //fetches a new leaderboar page
            $scope.getLeaderboardPage = function (contToken) {
                $scope.leaderboardReady = false;
                var path = 'api/statistics/leaderboard/' + $scope.showing;
                if (contToken)
                    path += '?contToken=' + contToken;

                $http.get($rootScope.fdApi + path).then(function (response) {
                    $.each(response.data.Items, function (ix, i) {
                        i.deltaIcon = '';
                        if (i.CurrentRank > i.PreviousRank)
                            i.deltaIcon = 'fa-caret-down';
                        if (i.CurrentRank < i.PreviousRank)
                            i.deltaIcon = 'fa-caret-up';

                        $scope.lastWeekName = i.EpisodeScores[0].EpisodeName;

                        if (i.EpisodeScores.length > 1) {
                            $scope.twoWeeksAgoName = i.EpisodeScores[1].EpisodeName;
                        }

                        $scope.leaderboard.push(i);
                    });

                    $scope.leaderboardReady = true;
                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $rootScope.showError('There was an issue loading the leaderboard.');
                });

            };


            //fetches initial statistics 
            $scope.getLandingStats = function () {

                $scope.statsReady = true;
            };

            $scope.toggleLb = function (mode) {
                $scope.leaderboard = [];
                $scope.showing = mode;
                $scope.getLeaderboardPage();
            }

            //init page
            $scope.leaderboardReady = false;
            $scope.statsReady = false;
            $scope.init = function () {
                //fetch leaderboard
                $scope.getLeaderboardPage();

                //fetch landing stats
                $scope.getLandingStats();
            };


            $scope.init();

        });
    }]);


})();