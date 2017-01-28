(function () {

    var app = angular.module('wdf.roster', ['ngRoute']);

    app.controller('rosterController', ['$scope', '$rootScope', '$http', '$routeParams', function ($scope, $rootScope, $http, $routeParams) {
        document.addEventListener("deviceready", function () {
            $http.defaults.headers.common.Authorization = 'Bearer ' + $rootScope.user.Token;



            $scope.showing = 'current';

            $scope.ready = false;
            $scope.init = function () {
                $scope.ready = false;

                var pid = $routeParams.personId;
                if (!pid)
                    pid = $rootScope.user.PersonId;
                $scope.loadingText = $scope.rosterLoadText();
                $http.get($rootScope.fdApi + 'api/roster/bulk/' + pid).then(function (response) {

                    $scope.isUser = response.data.Person.PersonId === $rootScope.user.PersonId;
                    $scope.noun = $scope.isUser ? 'You' : response.data.Person.Username;
                    $scope.characters = response.data.Characters;
                    $scope.show = response.data.RelatedShow;

                    $scope.viewingUser = response.data.Person;

                    $scope.score = $rootScope.generatePointValue($scope.viewingUser.TotalScore);

                    if ($scope.isUser) {
                        $rootScope.user.TotalScore = response.data.Person.TotalScore;

                        var allSlots = response.data.Slots;
                        $scope.slots = [];
                        $scope.history = [];
                        $scope.episode = response.data.CurrentEpisode;
                        $rootScope.episodes = response.data.AllEpisodes;

                        if (allSlots != null) {
                            $.each(allSlots, function (ix, i) {

                                if (i.EpisodeId === response.data.CurrentEpisode.id)
                                    $scope.slots.push(i);
                            });
                        }
                    }

                    $scope.history = [];
                    $.each(response.data.History, function (ix, h) {
                        h.score = $rootScope.generatePointValue(h.TotalScore);
                        $scope.history.push(h);
                    });

                    if ($scope.history.length > 0) {
                        $scope.gain = $scope.history[0].TotalScore;
                        $scope.verb = $scope.gain > 0 ? 'gained' : 'lost';
                    }
                    $scope.showing = $scope.isUser ? 'current' : 'history';

                    $scope.ready = true;

                    if ($scope.isUser) {
                        setTimeout(function () {

                            var animInClass = "flipInY";
                            var animOutClass = "bounceOut";

                            $('body').on('click', '.front', function () {
                                $(this).addClass('flipOutY');
                                $('#charModal').addClass(animInClass);
                            });

                            $('#charModal').on('hide.bs.modal', function () {

                            });

                            $('#charModal').on('hidden.bs.modal', function (evt) {
                                $('#charModal').removeClass(animOutClass)
                            });
                        }, 0);
                    }
                }).catch(function (error) {
                    $rootScope.handleError(error);
                    $scope.ready = false;
                    $rootScope.showError('There was a problem fetching your roster information.');
                });

            };

            $scope.toggleShowing = function () {
                $scope.showing = ($scope.showing === 'current') ? 'history' : 'current';
            };



            $scope.parseTimestamp = function (totalSeconds) {

                if (totalSeconds < 60)
                    return '00:' + totalSeconds;

                var mins = Math.floor(totalSeconds / 60);
                var secs = totalSeconds % 60;

                var minsStr = mins > 9 ? '' + mins : '0' + mins;
                var secStr = secs > 9 ? '' + secs : '0' + secs;
                return minsStr + ':' + secStr;
            };

            //helper for roster
            var findEpisodeById = function (id) {

                if (!$rootScope.episodes)
                    return null;

                var ep = null;
                $.each($rootScope.episodes, function (ix, i) {
                    if (i.id === id) {
                        ep = i;
                        return false;
                    }
                });

                return ep;
            };




            //flips a card over
            $scope.eSlot = null;
            $scope.flipSlot = function (slot) {

                if ($scope.eSlot === null) {
                    $scope.eSlot = slot;
                    $('#charModal').modal();
                } else {

                    $('#charModal').removeClass('flipInY').addClass('bounceOut');

                    $scope.eSlot = null;
                    setTimeout(function () {
                        $scope.working = false;
                        if (!$scope.$digest) $scope.$apply();

                        $('#charModal').modal('hide');
                        $('#' + slot.Id + ' .front').removeClass('flipOutY').addClass('flipInY');
                    }, 600);

                }
            };


            $rootScope.$on('flipSlot', function () {

                $scope.flipSlot($scope.eSlot);
            });

            var findSlotIx = function (id) {
                var sIx = -1;

                $.each($scope.slots, function (ix, i) {
                    if (i.Id === id) {
                        sIx = ix;
                        return false;
                    }
                });

                return sIx;
            };

            $scope.charIsSlotted = function (id) {
                if ($scope.slots === undefined)
                    return false;

                var results = $.grep($scope.slots, function (e, ix) {
                    if (!e.Pick)
                        return false;

                    return (e.Pick.CharacterId === id);
                });

                return results.length > 0;
            };

            $scope.working = false;
            $scope.removeChar = function (pickId) {
                $scope.working = true;
                var b64 = btoa(pickId);
                $http.delete($rootScope.fdApi + 'api/roster/pick/' + b64).then(function (response) {
                    var slotIx = findSlotIx($scope.eSlot.Id);
                    $scope.slots[slotIx] = response.data;
                    $scope.flipSlot($scope.slots[slotIx]);
                }).catch(function (error) {
                    $scope.flipSlot($scope.eSlot);
                    $rootScope.handleError(error);
                    $rootScope.showError(error);
                    $scope.working = false;
                });

            };

            $scope.selectChar = function (ch) {
                if (ch === null) {
                    return;
                }

                if ($scope.charIsSlotted(ch))
                    return;

                $scope.working = true;

                var slotType = $scope.eSlot.DeathSlot ? 1 : 0;

                var req = { CharacterId: ch, ShowId: $scope.show.id, SlotType: slotType };
                if ($scope.eSlot !== null && $scope.eSlot.Pick != null)
                    req.SwappingWithCharacterId = $scope.eSlot.Pick.CharacterId;

                $http.put($rootScope.fdApi + 'api/roster/pick', req)
                .then(function (response) {
                    var slotIx = findSlotIx($scope.eSlot.Id);
                    $scope.slots[slotIx] = response.data;
                    $scope.flipSlot($scope.slots[slotIx]);

                    if ($rootScope.user.askNotify) {
                        //if (true) {
                        $scope.askNotifyStep = 1;
                        $('#askNotify').modal();
                        $rootScope.user.askNotify = false;
                    }

                }).catch(function (error) {
                    $scope.flipSlot($scope.eSlot);
                    $rootScope.handleError(error);
                    $rootScope.showError(error);
                    $scope.working = false;
                });
            };

            $scope.setupNotify = function () {
                $rootScope.toggleNotifications(true);
                $rootScope.updateConfiguration('NotifyWhenScored', true);
                $rootScope.user.Configuration.NotifyWhenScored = true;
                $scope.askNotifyStep = 2;
            };

            $scope.dlh = $rootScope.generateDeadlineHoursOption();

            $scope.updateDeadlineHours = function (key, value) {
                $rootScope.setDeadlineHours($scope.dlh.hours)

                $scope.bailAskNotify(0);
            };

            $scope.bailAskNotify = function (step) {

                $scope.askNotifyStep = step;
                setTimeout(function () {
                    $('#askNotify').removeClass('fadeInDown').addClass('hinge');
                    setTimeout(function () {
                        $('#askNotify').removeClass('hinge').addClass('fadeInDown');
                        $('#askNotify').modal('hide');
                    }, 2500);
                }, 3000);
            };


            $scope.rosterLoadText = function () {

                var loadText = ['I wonder who died...', 'Loading roster...', 'Rostering the loader...', 'Thinking of a witty loading text...', 'Discovering character options...', 'Pondering possibility of a cure...', 'Fetching the humans we bet on...', 'Hiding behind Rick...', 'Feeding Lucille...'];

                var option = Math.floor(Math.random() * loadText.length);
                return loadText[option];
            };

            $scope.init();
        });
    }]);


})();