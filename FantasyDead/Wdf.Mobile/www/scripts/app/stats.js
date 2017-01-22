(function () {
    var app = angular.module('wdf.stats', ['ngRoute', 'chart.js']);

    app.controller('statsController', ['$scope', '$rootScope', '$location', '$http', function ($scope, $rootScope, $location, $http) {
        document.addEventListener("deviceready", function () {
            $http.defaults.headers.common.Authorization = 'Bearer ' + $rootScope.user.Token;


            var hasId = function (col, id) {
                var fIx = -1;
                $.each(col, function (ix, i) {
                    if (i.id === id) {
                        fIx = ix;
                        return true;
                    }
                });

                return fIx !== -1;
            };

            var isLit = function (epId) {
                if (!$scope.episodes)
                    return false;

                var lit = true;
                $.each($scope.episodes, function (ix, e) {
                    if (e.id === epId) {
                        lit = e.lit;
                        return true;
                    }
                });

                return lit;
            };

            var has = function (col, id) {
                var fIx = -1;
                $.each(col, function (ix, i) {
                    if (i === id) {
                        fIx = ix;
                        return true;
                    }
                });

                return fIx !== -1;
            };

            $scope.ready = false;
            //init page
            $scope.init = function () {
                Chart.defaults.global.legend.display = true;
                Chart.defaults.global.legend.position = 'bottom';
                Chart.defaults.global.colors[0] = '#9c661f';
                Chart.defaults.global.colors[1] = '#dfae74';
                Chart.defaults.global.colors[2] = '#dcdcdc';
                Chart.defaults.global.colors[3] = '#777777';
                Chart.defaults.global.colors[4] = '#f7464a';
                Chart.defaults.global.colors[5] = '#9e0000';
                Chart.defaults.global.colors[6] = '#000000';


                $http.get($rootScope.fdApi + 'api/statistics/events/all').then(function (response) {

                    $scope.rawData = response.data;
                    $scope.formulateData(response.data);
                    $scope.ready = true;
                }).catch(function (error) {
                    $rootScope.showError('There was an error fetching the stats...');
                });
            };

            $scope.applyChanges = function () {
                $('#epModal').modal('hide');
                $scope.formulateData($scope.rawData, true);
            }

            $scope.formulateData = function (rawEvents, filtered) {
                if (filtered === undefined)
                    filtered = false;


                $scope.seasonEv = { labels: [], data: [] };
                $scope.charLb = { options: { legend: { display: false } }, labels: [], data: [] };

                if (!filtered) {
                    $scope.episodes = [];
                }

                var tempActions = {};
                var tempChars = {};

                $.each(rawEvents, function (ix, ev) {

                    if (!filtered) {
                        //episodes
                        if (!hasId($scope.episodes, ev.EpisodeId)) {
                            $scope.episodes.push({ id: ev.EpisodeId, name: ev.EpisodeName, lit: true });
                        }
                    } else if (filtered && !isLit(ev.EpisodeId)) {
                        console.log(JSON.stringify(ev));
                        return false;
                    }

                    //actions
                    if (!has($scope.seasonEv.labels, ev.ActionType)) {
                        $scope.seasonEv.labels.push(ev.ActionType);
                        tempActions[ev.ActionType] = 0;
                    }

                    tempActions[ev.ActionType]++;

                    //chars
                    if (!has($scope.charLb.labels, ev.CharacterName)) {
                        $scope.charLb.labels.push(ev.CharacterName);
                        tempChars[ev.CharacterName] = 0;
                    }

                    tempChars[ev.CharacterName] += ev.Points;

                });

                //aggregate season events
                $.each($scope.seasonEv.labels, function (ix, l) {
                    $scope.seasonEv.data.push(tempActions[l]);
                });

                //aggregate character points
                $.each($scope.charLb.labels, function (ix, l) {
                    $scope.charLb.data.push(tempChars[l]);
                });

            }


            $scope.changeEpModal = function () {
                $('#epModal').modal();
            };

            var changeColor = function (chart) {
                var ctx = chart.chart.ctx;
                var gradient = ctx.createLinearGradient(0, 0, 0, 400);
                gradient.addColorStop(0, 'rgba(0,0,0,1)');
                gradient.addColorStop(1, 'rgba(125,125,125,1)');
                chart.chart.config.data.datasets[0].backgroundColor = gradient;
                chart.chart.config.data.datasets[0].borderColor = gradient;
            };

            $scope.$on('chart-create', function (evt, chart) {
                if (chart.chart.canvas.id === 'char-chart') {
                    changeColor(chart);
                    chart.update();
                };
            });

            $scope.init();

        });
    }]);


})();